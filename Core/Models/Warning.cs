using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>Core-owned warning instances published on per-kind database events.</summary>
   internal abstract class WarningBase : IWarning
   {
      public string Source => WarningKinds.SourceCore;

      public abstract string Kind { get; }

      public abstract WarningSeverity Severity { get; }
   }

   internal sealed class ActivityReviewWarning : WarningBase, IActivityReviewWarning
   {
      public ActivityReviewWarning(IActivity[] activities)
      {
         Activities = activities;
         Severity = activities.Length == 0
            ? WarningSeverity.Info
            : activities.Max(static a => SeverityFor(a.EventType));
      }

      public override string Kind => WarningKinds.ActivityReview;

      public override WarningSeverity Severity { get; }

      public IEnumerable<IActivity> Activities { get; }

      /// <summary>
      /// Per-event severity; the ActivityReview bucket uses the max across
      /// its NeedsReview rows.
      /// </summary>
      internal static WarningSeverity SeverityFor(ActivityEventType eventType)
         => eventType switch
         {
            ActivityEventType.LoginFailed
               or ActivityEventType.ActivityLogTampered
               or ActivityEventType.LoginSessionTimeoutReached
               or ActivityEventType.ExportingDataStarted
               or ActivityEventType.ExportingDataSucceded
               or ActivityEventType.ExportingDataFailed
               => WarningSeverity.Critical,

            ActivityEventType.ImportingDataStarted
               or ActivityEventType.ImportingDataSucceded
               or ActivityEventType.ImportingDataFailed
               or ActivityEventType.ItemAdded
               or ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile
               or ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile
               or ActivityEventType.DontMergeAndRemoveAutoSaveFile
               or ActivityEventType.DontMergeAndKeepAutoSaveFile
               => WarningSeverity.Info,

            // ItemUpdated, ItemDeleted, and any other NeedsReview row.
            _ => WarningSeverity.Warning,
         };
   }

   internal sealed class AccountsWarning : WarningBase, IPasswordUpdateReminderWarning, IDuplicatedPasswordsWarning,
      IPasswordLeakedWarning, IWeakAccountPasswordWarning, IPasskeyReuseWarning
   {
      public AccountsWarning(string kind, WarningSeverity severity, IAccount[] accounts)
      {
         Kind = kind;
         Severity = severity;
         Accounts = accounts;
      }

      public override string Kind { get; }

      public override WarningSeverity Severity { get; }

      public IEnumerable<IAccount> Accounts { get; }
   }

   internal sealed class VaultSecuritySettingsWarning : WarningBase, IVaultSecuritySettingsWarning
   {
      public VaultSecuritySettingsWarning(SecuritySettingsIssue issues)
         => Issues = issues;

      public override string Kind => WarningKinds.VaultSecuritySettings;

      public override WarningSeverity Severity => WarningSeverity.Warning;

      public SecuritySettingsIssue Issues { get; }
   }

   internal sealed class InsufficientPasskeysWarning : WarningBase, IInsufficientPasskeysWarning
   {
      public InsufficientPasskeysWarning(int count, int recommendedMinimum)
      {
         Count = count;
         RecommendedMinimum = recommendedMinimum;
         Severity = count <= 1 ? WarningSeverity.Critical : WarningSeverity.Warning;
      }

      public override string Kind => WarningKinds.InsufficientPasskeys;

      public override WarningSeverity Severity { get; }

      public int Count { get; }

      public int RecommendedMinimum { get; }
   }

   internal sealed class WeakPasskeyWarning : WarningBase, IWeakPasskeyWarning
   {
      public WeakPasskeyWarning(IReadOnlyList<int> passkeyIndexes, SecretQualityIssue issues)
      {
         PasskeyIndexes = passkeyIndexes;
         Issues = issues;
      }

      public override string Kind => WarningKinds.WeakPasskey;

      public override WarningSeverity Severity => WarningSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; }

      public SecretQualityIssue Issues { get; }
   }

   internal sealed class PasskeyLeakedWarning : WarningBase, IPasskeyLeakedWarning
   {
      public PasskeyLeakedWarning(IReadOnlyList<int> passkeyIndexes)
         => PasskeyIndexes = passkeyIndexes;

      public override string Kind => WarningKinds.PasskeyLeaked;

      public override WarningSeverity Severity => WarningSeverity.Critical;

      public IReadOnlyList<int> PasskeyIndexes { get; }
   }
}
