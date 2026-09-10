using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>Core-owned alert instances published on per-kind database events.</summary>
   internal abstract class AlertBase : IAlert
   {
      public string Source => AlertKinds.SourceCore;

      public abstract string Kind { get; }

      public abstract AlertSeverity Severity { get; }
   }

   internal sealed class ActivityReviewAlert : AlertBase, IActivityReviewAlert
   {
      public ActivityReviewAlert(IActivity[] activities)
      {
         Activities = activities;
         Severity = activities.Length == 0
            ? AlertSeverity.Info
            : activities.Max(static a => SeverityFor(a.EventType));
      }

      public override string Kind => AlertKinds.ActivityReview;

      public override AlertSeverity Severity { get; }

      public IEnumerable<IActivity> Activities { get; }

      /// <summary>
      /// Per-event severity; the ActivityReview bucket uses the max across
      /// its NeedsReview rows.
      /// </summary>
      internal static AlertSeverity SeverityFor(ActivityEventType eventType)
         => eventType switch
         {
            ActivityEventType.LoginFailed
               or ActivityEventType.ActivityLogTampered
               or ActivityEventType.LoginSessionTimeoutReached
               or ActivityEventType.ExportingDataStarted
               or ActivityEventType.ExportingDataSucceded
               or ActivityEventType.ExportingDataFailed
               => AlertSeverity.Critical,

            ActivityEventType.ImportingDataStarted
               or ActivityEventType.ImportingDataSucceded
               or ActivityEventType.ImportingDataFailed
               or ActivityEventType.ItemAdded
               or ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile
               or ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile
               or ActivityEventType.DontMergeAndRemoveAutoSaveFile
               or ActivityEventType.DontMergeAndKeepAutoSaveFile
               => AlertSeverity.Info,

            // ItemUpdated, ItemDeleted, and any other NeedsReview row.
            _ => AlertSeverity.Warning,
         };
   }

   internal sealed class AccountsAlert : AlertBase, IPasswordUpdateReminderAlert, IDuplicatedPasswordsAlert,
      IPasswordLeakedAlert, IWeakAccountPasswordAlert, IPasskeyReuseAlert
   {
      public AccountsAlert(string kind, AlertSeverity severity, IAccount[] accounts)
      {
         Kind = kind;
         Severity = severity;
         Accounts = accounts;
      }

      public override string Kind { get; }

      public override AlertSeverity Severity { get; }

      public IEnumerable<IAccount> Accounts { get; }
   }

   internal sealed class VaultSecuritySettingsAlert : AlertBase, IVaultSecuritySettingsAlert
   {
      public VaultSecuritySettingsAlert(SecuritySettingsIssue issues)
         => Issues = issues;

      public override string Kind => AlertKinds.VaultSecuritySettings;

      public override AlertSeverity Severity => AlertSeverity.Warning;

      public SecuritySettingsIssue Issues { get; }
   }

   internal sealed class InsufficientPasskeysAlert : AlertBase, IInsufficientPasskeysAlert
   {
      public InsufficientPasskeysAlert(int count, int recommendedMinimum)
      {
         Count = count;
         RecommendedMinimum = recommendedMinimum;
         Severity = count <= 1 ? AlertSeverity.Critical : AlertSeverity.Warning;
      }

      public override string Kind => AlertKinds.InsufficientPasskeys;

      public override AlertSeverity Severity { get; }

      public int Count { get; }

      public int RecommendedMinimum { get; }
   }

   internal sealed class WeakPasskeyAlert : AlertBase, IWeakPasskeyAlert
   {
      public WeakPasskeyAlert(IReadOnlyList<int> passkeyIndexes, SecretQualityIssue issues)
      {
         PasskeyIndexes = passkeyIndexes;
         Issues = issues;
      }

      public override string Kind => AlertKinds.WeakPasskey;

      public override AlertSeverity Severity => AlertSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; }

      public SecretQualityIssue Issues { get; }
   }

   internal sealed class PasskeyLeakedAlert : AlertBase, IPasskeyLeakedAlert
   {
      public PasskeyLeakedAlert(IReadOnlyList<int> passkeyIndexes)
         => PasskeyIndexes = passkeyIndexes;

      public override string Kind => AlertKinds.PasskeyLeaked;

      public override AlertSeverity Severity => AlertSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; }
   }
}
