using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal sealed class ActivityCenter : IDisposable
   {
      internal Database Database
      {
         get => field ?? throw new NullValueException(nameof(Database));
         set;
      }

      internal List<IActivity> Activities = [];

      public List<string> ActivityList { get; set; } = [];

      public string Username { get; set; } = string.Empty;

      public string PublicKey { get; set; } = string.Empty;

      // Tamper-evidence for the activity log. The log must accept entries even
      // when no user is logged in (e.g. failed logins), so writing uses only the
      // public key and cannot be protected by a secret. Instead, on every save
      // made while a user is logged in, the whole current log is sealed with an
      // RSA signature over its entries (see _seal). SealedCount records how many
      // entries that signature covers; entries appended before the next login
      // form an unsealed tail. Verification (see VerifyIntegrity) then detects
      // any modification, forgery, reordering, key substitution or rollback of
      // the sealed portion. Legacy files (created before sealing existed) have an
      // empty Signature and a zero SealedCount.
      public string Signature { get; set; } = string.Empty;

      public int SealedCount { get; set; }

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

         Activities.Insert(0, activity);
         ActivityList.Insert(0, Database.CryptographyCenter.EncryptAsymmetrically(activity.ToString(), PublicKey));

         if (Database.User is null)
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
         Activities.Clear();

         if (Database.User is null) return;

         // Decryption is tolerant: an entry that cannot be decrypted (e.g. one
         // forged with a different key) is skipped rather than aborting login.
         // Authenticity of the sealed portion is asserted separately by
         // VerifyIntegrity, which does not need to decrypt anything.
         Activities = [.. ActivityList.AsParallel()
            .Select(_tryDecrypt)
            .Where(x => x is not null)
            .Cast<Activity>()
            .OrderByDescending(x => x.DateTime)];
      }

      private Activity? _tryDecrypt(string encryptedActivity)
      {
         if (Database.User is null) return null;

         try
         {
            return new Activity(Database.CryptographyCenter.DecryptAsymmetrically(encryptedActivity, Database.User.PrivateKey.Reveal()));
         }
#pragma warning disable CA1031 // Intentional: any decryption failure means a skipped entry, not a login abort
         catch (Exception ex)
#pragma warning restore CA1031
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
         if (Database.User is null) throw new NullValueException(nameof(Database.User));

         int watermark = Database.User.ActivitySealWatermark;

         // Never sealed (legacy file, or a brand-new database before its first
         // save): there is nothing to verify.
         if (watermark == 0 && string.IsNullOrEmpty(Signature))
         {
            return true;
         }

         // The database (which is tamper-proof) records that the log was sealed,
         // but the signature is now gone: a downgrade/strip attempt.
         if (string.IsNullOrEmpty(Signature))
         {
            return false;
         }

         // Fewer sealed entries than the trusted database recorded, or a list
         // shorter than its own sealed count: truncation/rollback.
         if (SealedCount < watermark || ActivityList.Count < SealedCount)
         {
            return false;
         }

         // The public key stored in the (unencrypted) log must be the one that
         // belongs to the private key held in the encrypted database. This
         // defeats an attacker swapping in their own key pair.
         string trustedPublicKey = Database.CryptographyCenter.GetPublicKey(Database.User.PrivateKey.Reveal());
         return trustedPublicKey == PublicKey && Database.CryptographyCenter.Verify(_canonicalSealedContent(), Signature, trustedPublicKey);
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
         if (rebuildStringActivities)
         {
            _removeOldActivities();

            ActivityList.Clear();
            ActivityList.AddRange(Activities
               .OrderByDescending(x => x.DateTime)
               .Select(x => ((Activity)x).ToString())
               .Distinct()
               .Select(x => Database.CryptographyCenter.EncryptAsymmetrically(x, PublicKey)));
         }

         _seal();

         Database.FileLocker.Save(this, Database.ActivityFileEntry);
      }

      private void _seal()
      {
         // Sealing needs the private key, which is only available once a user is
         // logged in. Activities appended before login grow an unsealed tail
         // that is sealed by the next save after a successful login.
         if (Database.User is null)
         {
            return;
         }

         SealedCount = ActivityList.Count;
         Signature = Database.CryptographyCenter.Sign(_canonicalSealedContent(), Database.User.PrivateKey.Reveal());
      }

      private string _canonicalSealedContent()
      {
         // Entries are stored newest-first, so the sealed set (everything that
         // existed at the last seal) is the tail; anything prepended afterwards
         // is the unsealed part and is excluded here.
         IEnumerable<string> sealedEntries = ActivityList.Skip(ActivityList.Count - SealedCount);

         return string.Join("\n", [$"{SealedCount}", PublicKey, .. sealedEntries]);
      }

      private void _removeOldActivities()
      {
         if (Database.User is null
            || Database.User.Settings.NumberOfMonthActivitiesToKeep == 0)
         {
            return;
         }

         DateTime limitDate = DateTime.Now.AddMonths(-Database.User.Settings.NumberOfMonthActivitiesToKeep).Date.AddDays(-DateTime.Now.Day + 1);
         Activities = [.. Activities.Where(x => x.DateTime >= limitDate || x.NeedsReview)];
      }
   }
}
