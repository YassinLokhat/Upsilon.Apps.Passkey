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

   internal sealed class ActivityReviewAlert(IActivity[] activities) : AlertBase, IActivityReviewAlert
   {
      public override string Kind => AlertKinds.ActivityReview;

      public override AlertSeverity Severity { get; } = activities.Length == 0
            ? AlertSeverity.Info
            : activities.Max(static a => SeverityFor(a.EventType));

      public IEnumerable<IActivity> Activities { get; } = activities;

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
               or ActivityEventType.ExportingDataSucceeded
               or ActivityEventType.ExportingDataFailed
               => AlertSeverity.Critical,

            ActivityEventType.ImportingDataStarted
               or ActivityEventType.ImportingDataSucceeded
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

   internal sealed class AccountsAlert(string kind, AlertSeverity severity, IAccount[] accounts) : AlertBase, IPasswordUpdateReminderAlert, IDuplicatedPasswordsAlert,
      IPasswordLeakedAlert, IWeakAccountPasswordAlert, IPasskeyReuseAlert
   {
      public override string Kind { get; } = kind;

      public override AlertSeverity Severity { get; } = severity;

      public IEnumerable<IAccount> Accounts { get; } = accounts;
   }

   internal sealed class VaultSecuritySettingsAlert(SecuritySettingsIssue issues) : AlertBase, IVaultSecuritySettingsAlert
   {
      public override string Kind => AlertKinds.VaultSecuritySettings;

      public override AlertSeverity Severity => AlertSeverity.Warning;

      public SecuritySettingsIssue Issues { get; } = issues;
   }

   internal sealed class InsufficientPasskeysAlert(int count, int recommendedMinimum) : AlertBase, IInsufficientPasskeysAlert
   {
      public override string Kind => AlertKinds.InsufficientPasskeys;

      public override AlertSeverity Severity { get; } = count <= 1 ? AlertSeverity.Critical : AlertSeverity.Warning;

      public int Count { get; } = count;

      public int RecommendedMinimum { get; } = recommendedMinimum;
   }

   internal sealed class WeakPasskeyAlert(IReadOnlyList<int> passkeyIndexes, SecretQualityIssue issues) : AlertBase, IWeakPasskeyAlert
   {
      public override string Kind => AlertKinds.WeakPasskey;

      public override AlertSeverity Severity => AlertSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; } = passkeyIndexes;

      public SecretQualityIssue Issues { get; } = issues;
   }

   internal sealed class PasskeyLeakedAlert(IReadOnlyList<int> passkeyIndexes) : AlertBase, IPasskeyLeakedAlert
   {
      public override string Kind => AlertKinds.PasskeyLeaked;

      public override AlertSeverity Severity => AlertSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; } = passkeyIndexes;
   }
}
