using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Tamper-evident activity log: RSA-hybrid per record, seal on save while
   /// logged in, deferred ZIP writes. See SECURITY.md ("Activity-log integrity").
   /// </summary>
   internal sealed class ActivityCenter : IDisposable
   {
      internal Database Database
      {
         get => field ?? throw new NullValueException(nameof(Database));
         set;
      }

      // In-memory decrypted view. Mutated on the UI thread and read by warning
      // scans / Flush; always accessed under _gate.
      internal List<IActivity> Activities = [];

      // Serialized ciphertexts (and seal metadata below). Same gate as Activities
      // so a deferred persist never snapshots a torn list.
      public List<string> ActivityList { get; set; } = [];

      public string Username { get; set; } = string.Empty;

      public string PublicKey { get; set; } = string.Empty;

      // Tamper-evidence for the activity log. The log must accept entries even
      // when no user is logged in (e.g. failed logins), so writing uses only the
      // public key and cannot be protected by a secret. Instead, on every save
      // made while a user is logged in, the current log is sealed with an RSA
      // signature over its entries (see _seal). Ciphertexts are produced once at
      // AddActivity time; Save only re-encrypts when retention pruning drops
      // rows. SealedCount records how many entries that signature covers; entries
      // appended before the next login form an unsealed tail. Verification (see
      // VerifyIntegrity) then detects any modification, forgery, reordering, key
      // substitution or rollback of the sealed portion.
      public string Signature { get; set; } = string.Empty;

      public int SealedCount { get; set; }

      // Protects Activities, ActivityList, Signature, SealedCount, and the
      // Username/PublicKey reads used while mutating those collections. Lock
      // order when nested: AutoSave → ActivityCenter → FileLocker.
      private readonly Lock _gate = new();

      // Each AddActivity used to rewrite the ZIP activity entry (and RSA-sign it
      // when logged in). Bursts of edits therefore paid N full archive writes.
      // While a user is logged in we coalesce those into one flush; pre-login
      // events still write immediately so failed-login / open audits survive a
      // crash before the session starts.
      private readonly DeferredPersistence _deferred;

      public ActivityCenter()
      {
         _deferred = new DeferredPersistence(() => _persist(rebuildStringActivities: false));
      }

      internal void AddActivity(string itemId, ActivityEventType eventType, string[] data, bool needsReview)
      {
         Activity activity = new(DateTime.Now.Ticks, itemId, eventType, data, needsReview);

         // Capture the public key under the gate, encrypt outside it, then insert
         // both plaintext and ciphertext atomically. Holding _gate across RSA
         // would stall concurrent Flush/warning reads for no consistency gain.
         string publicKey;
         lock (_gate)
         {
            publicKey = PublicKey;
         }

         string encrypted = Database.CryptographyCenter.EncryptAsymmetrically(activity.ToString(), publicKey);

         bool flushImmediately;
         lock (_gate)
         {
            Activities.Insert(0, activity);
            ActivityList.Insert(0, encrypted);
            flushImmediately = Database.User is null;
         }

         if (flushImmediately)
         {
            // No session yet: flush now so the audit trail is on disk even if the
            // process dies before Login (failed attempts, DatabaseOpened, …).
            Save(rebuildStringActivities: false);
         }
         else
         {
            _deferred.Schedule();
         }
      }

      internal void LoadStringActivities()
      {
         if (Database.User is null)
         {
            lock (_gate)
            {
               Activities.Clear();
            }

            return;
         }

         // Snapshot ciphertexts under the gate, decrypt outside (RSA is slow and
         // must not block AddActivity / Flush), then publish the result under the
         // gate again. Authenticity of the sealed portion is asserted separately
         // by VerifyIntegrity, which does not need to decrypt anything.
         string[] encryptedSnapshot;
         lock (_gate)
         {
            encryptedSnapshot = [.. ActivityList];
         }

         // Preserve ActivityList order (newest-first). AsOrdered keeps that
         // sequence across parallel decrypts; sorting by DateTime alone was
         // unstable when several entries shared the same tick.
         List<IActivity> decrypted = [.. encryptedSnapshot.AsParallel()
            .AsOrdered()
            .Select(_tryDecrypt)
            .Where(x => x is not null)
            .Cast<Activity>()];

         lock (_gate)
         {
            Activities = decrypted;
         }
      }

      private Activity? _tryDecrypt(string encryptedActivity)
      {
         if (Database.User is null)
         {
            return null;
         }

         try
         {
            return new Activity(Database.CryptographyCenter.DecryptAsymmetrically(encryptedActivity, Database.User.PrivateKey.Reveal()));
         }
         catch (Exception ex)
            when (ex is CorruptedSourceException
            or WrongPasswordException
            or ArgumentNullException)
         {
            // An entry that cannot be decrypted (e.g. one forged with a different
            // key) is skipped rather than aborting login; authenticity of the
            // sealed portion is asserted separately by VerifyIntegrity. We still
            // trace the failure so a skipped entry is diagnosable.
            System.Diagnostics.Trace.TraceWarning($"Activity entry could not be decrypted and was skipped: {ex}");
            return null;
         }
      }

      /// <summary>
      /// Verifies that the sealed portion of the log has not been tampered with.
      /// Requires a logged-in user (the private key anchors the check). Never
      /// throws: returns <see langword="false"/> when tampering is detected.
      /// </summary>
      internal bool VerifyIntegrity()
      {
         if (Database.User is null)
         {
            throw new NullValueException(nameof(Database.User));
         }

         int watermark = Database.User.ActivitySealWatermark;
         string signature;
         int sealedCount;
         int activityCount;
         string publicKey;
         string canonical;

         lock (_gate)
         {
            signature = Signature;
            sealedCount = SealedCount;
            activityCount = ActivityList.Count;
            publicKey = PublicKey;

            // A brand-new database before its first save has nothing sealed yet.
            if (watermark == 0 && string.IsNullOrEmpty(signature))
            {
               return true;
            }

            // The database (which is tamper-proof) records that the log was sealed,
            // but the signature is now gone: a downgrade/strip attempt.
            if (string.IsNullOrEmpty(signature))
            {
               return false;
            }

            // Fewer sealed entries than the trusted database recorded, or a list
            // shorter than its own sealed count: truncation/rollback.
            if (sealedCount < watermark || activityCount < sealedCount)
            {
               return false;
            }

            canonical = _canonicalSealedContent_NoLock();
         }

         // The public key stored in the (unencrypted) log must be the one that
         // belongs to the private key held in the encrypted database. This
         // defeats an attacker swapping in their own key pair. RSA verify runs
         // outside the gate.
         string trustedPublicKey = Database.CryptographyCenter.GetPublicKey(Database.User.PrivateKey.Reveal());
         return trustedPublicKey == publicKey && Database.CryptographyCenter.Verify(canonical, signature, trustedPublicKey);
      }

      /// <summary>
      /// Snapshot of activities newest-first for UI / public API consumers.
      /// Insertion order is already newest-first; do not re-sort by DateTime
      /// (ties would be unstable).
      /// </summary>
      internal IActivity[] GetActivitiesOrdered()
      {
         lock (_gate)
         {
            return [.. Activities];
         }
      }

      /// <summary>
      /// Snapshot of activities that still need user review (warning scan).
      /// </summary>
      internal IActivity[] GetActivitiesNeedingReview()
      {
         lock (_gate)
         {
            return [.. Activities.Where(x => x.NeedsReview)];
         }
      }

      /// <summary>
      /// Current seal watermark, safe to read while a flush may be in progress.
      /// </summary>
      internal int GetSealedCount()
      {
         lock (_gate)
         {
            return SealedCount;
         }
      }

      /// <summary>
      /// Forces any debounced activity write to disk. Must run while the user is
      /// still available so the seal can be (re)computed.
      /// </summary>
      internal void Flush() => _deferred.Flush();

      /// <summary>
      /// Drops a pending debounced write without touching the disk.
      /// </summary>
      internal void CancelPending() => _deferred.Cancel();

      internal void Save(bool rebuildStringActivities)
      {
         // An explicit save supersedes any pending debounce.
         _deferred.Cancel();
         _persist(rebuildStringActivities);
      }

      public void Dispose() => _deferred.Dispose();

      private void _persist(bool rebuildStringActivities)
      {
         // Append path (deferred flushes, AddActivity): ActivityList already holds
         // per-entry ciphertexts from EncryptAsymmetrically at insert time, so we
         // only reseal and write. Rebuild path (explicit Save / retention change):
         // prune first, and re-RSA only when entries were actually dropped —
         // otherwise Save would pay O(n) RSA for an unchanged log.
         // FileLocker is taken inside the same critical section (lock order:
         // ActivityCenter → FileLocker).
         lock (_gate)
         {
            if (rebuildStringActivities && _removeOldActivities_NoLock())
            {
               _rebuildActivityList_NoLock();
            }

            _seal_NoLock();

            Database.FileLocker.Save(this, Database.ActivityFileEntry);
         }
      }

      // Caller must hold _gate. Re-encrypts the current Activities view into
      // ActivityList (newest-first). Used only after a prune that dropped rows.
      private void _rebuildActivityList_NoLock()
      {
         ActivityList.Clear();
         ActivityList.AddRange(Activities
            .OrderByDescending(x => x.DateTime)
            .Select(x => Database.CryptographyCenter.EncryptAsymmetrically(((Activity)x).ToString(), PublicKey)));
      }

      // Caller must hold _gate.
      private void _seal_NoLock()
      {
         // Sealing needs the private key, which is only available once a user is
         // logged in. Activities appended before login grow an unsealed tail
         // that is sealed by the next save after a successful login.
         if (Database.User is null)
         {
            return;
         }

         SealedCount = ActivityList.Count;
         Signature = Database.CryptographyCenter.Sign(_canonicalSealedContent_NoLock(), Database.User.PrivateKey.Reveal());
      }

      // Caller must hold _gate.
      private string _canonicalSealedContent_NoLock()
      {
         // Entries are stored newest-first, so the sealed set (everything that
         // existed at the last seal) is the tail; anything prepended afterwards
         // is the unsealed part and is excluded here.
         IEnumerable<string> sealedEntries = ActivityList.Skip(ActivityList.Count - SealedCount);

         return string.Join("\n", [$"{SealedCount}", PublicKey, .. sealedEntries]);
      }

      // Caller must hold _gate. Returns true when at least one entry was dropped
      // so the caller knows ActivityList must be rebuilt to match.
      private bool _removeOldActivities_NoLock()
      {
         if (Database.User is null
            || Database.User.Settings.NumberOfMonthActivitiesToKeep == 0)
         {
            return false;
         }

         DateTime limitDate = DateTime.Now.AddMonths(-Database.User.Settings.NumberOfMonthActivitiesToKeep).Date.AddDays(-DateTime.Now.Day + 1);
         int before = Activities.Count;
         Activities = [.. Activities.Where(x => x.DateTime >= limitDate || x.NeedsReview)];
         return Activities.Count != before;
      }
   }
}
