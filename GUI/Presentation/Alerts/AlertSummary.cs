using System.Numerics;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Alerts
{
   /// <summary>Alert menu entries, declared in their tie-break display order.</summary>
   internal enum AlertGroup
   {
      LeakedPasswords,
      PasskeyQuality,
      ExpiredPasswords,
      WeakPasswords,
      ActivityReview,
      DuplicatedPasswords,
      SecuritySettings,
   }

   internal sealed record AlertGroupSummary(AlertGroup Group, AlertSeverity Severity, int Count);

   /// <summary>
   /// Counts and severities for the alerts menu. Holds no item labels so it
   /// can outlive the session safely.
   /// </summary>
   internal sealed class AlertSummary
   {
      private readonly Dictionary<AlertGroup, AlertGroupSummary> _groups;

      private AlertSummary(AlertSeverity severity, IReadOnlyList<AlertGroupSummary> ordered)
      {
         Severity = severity;
         OrderedGroups = ordered;
         _groups = ordered.ToDictionary(x => x.Group);
         TotalCount = ordered.Sum(x => x.Count);
      }

      public static AlertSummary Empty { get; } = From([]);

      public int TotalCount { get; }

      public AlertSeverity Severity { get; }

      /// <summary>
      /// Non-empty groups first, Critical then Warning then Info; equal severity
      /// keeps <see cref="AlertGroup"/> order. Empty groups follow.
      /// </summary>
      public IReadOnlyList<AlertGroupSummary> OrderedGroups { get; }

      public AlertGroupSummary this[AlertGroup group] => _groups[group];

      public static AlertSummary From(IEnumerable<IAlert> alerts)
      {
         ArgumentNullException.ThrowIfNull(alerts);
         IAlert[] all = [.. alerts];

         AlertGroupSummary[] groups = [.. Enum.GetValues<AlertGroup>().Select(group =>
         {
            IAlert[] members = [.. all.Where(x => _belongsTo(x, group))];
            return new AlertGroupSummary(group, AlertBroker.MaxSeverity(members), _count(group, members));
         })];

         AlertGroupSummary[] ordered =
         [
            .. groups.Where(static g => g.Count > 0).OrderByDescending(static g => g.Severity),
            .. groups.Where(static g => g.Count == 0),
         ];

         return new AlertSummary(AlertBroker.MaxSeverity(all), ordered);
      }

      private static bool _belongsTo(IAlert alert, AlertGroup group) => group switch
      {
         AlertGroup.ActivityReview => alert.Kind == AlertKinds.ActivityReview,
         AlertGroup.ExpiredPasswords => alert.Kind == AlertKinds.PasswordUpdateReminder,
         AlertGroup.DuplicatedPasswords => alert.Kind == AlertKinds.DuplicatedPasswords,
         AlertGroup.LeakedPasswords => alert.Kind == AlertKinds.PasswordLeaked,
         AlertGroup.WeakPasswords => alert.Kind == AlertKinds.WeakAccountPassword,
         AlertGroup.SecuritySettings => alert.Kind is AlertKinds.VaultSecuritySettings or AlertKinds.HostSecuritySettings,
         AlertGroup.PasskeyQuality => alert.Kind is AlertKinds.InsufficientPasskeys
            or AlertKinds.WeakPasskey
            or AlertKinds.PasskeyLeaked
            or AlertKinds.PasskeyReusedAsAccountPassword,
         _ => false,
      };

      private static int _count(AlertGroup group, IAlert[] members) => group switch
      {
         AlertGroup.ActivityReview => members.OfType<IActivityReviewAlert>().Sum(x => x.Activities.Count()),
         AlertGroup.ExpiredPasswords or AlertGroup.LeakedPasswords or AlertGroup.WeakPasswords
            => members.OfType<IAccountsAlert>().Sum(x => x.Accounts.Count()),
         AlertGroup.DuplicatedPasswords => members.Length,
         AlertGroup.SecuritySettings
            => members.OfType<IVaultSecuritySettingsAlert>().Sum(x => BitOperations.PopCount((uint)x.Issues))
               + members.OfType<IHostSecuritySettingsAlert>().Sum(x => BitOperations.PopCount((uint)x.Issues)),
         AlertGroup.PasskeyQuality => members.Count(x => x is IInsufficientPasskeysAlert
            or IWeakPasskeyAlert
            or IPasskeyLeakedAlert
            or IPasskeyReuseAlert),
         _ => 0,
      };
   }
}
