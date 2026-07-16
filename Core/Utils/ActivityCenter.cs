using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal sealed class ActivityCenter
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

      internal void AddActivity(string itemId, ActivityEventType eventType, string[] data, bool needsReview)
      {
         Activity activity = new(DateTime.Now.Ticks, itemId, eventType, data, needsReview);

         Activities.Insert(0, activity);
         ActivityList.Insert(0, Database.CryptographyCenter.EncryptAsymmetrically(activity.ToString(), PublicKey));

         Save(rebuildStringActivities: false);
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
            return new Activity(Database.CryptographyCenter.DecryptAsymmetrically(encryptedActivity, Database.User.PrivateKey));
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
         string trustedPublicKey = Database.CryptographyCenter.GetPublicKey(Database.User.PrivateKey);
         return trustedPublicKey == PublicKey && Database.CryptographyCenter.Verify(_canonicalSealedContent(), Signature, trustedPublicKey);
      }

      internal void Save(bool rebuildStringActivities)
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
         Signature = Database.CryptographyCenter.Sign(_canonicalSealedContent(), Database.User.PrivateKey);
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
            || Database.User.NumberOfMonthActivitiesToKeep == 0)
         {
            return;
         }

         DateTime limitDate = DateTime.Now.AddMonths(-Database.User.NumberOfMonthActivitiesToKeep).Date.AddDays(-DateTime.Now.Day + 1);
         Activities = [.. Activities.Where(x => x.DateTime >= limitDate || x.NeedsReview)];
      }
   }
}
