using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      // Open and Save each queue a scan; a slow leak check from an earlier queue
      // must not overwrite results from a newer one once it finally finishes.
      private int _alertScanGeneration;
      private readonly object _alertScanGate = new();

      private readonly Dictionary<string, IReadOnlyList<IAlert>> _coreAlerts = new(StringComparer.Ordinal);

      public IReadOnlyDictionary<string, IReadOnlyList<IAlert>> CoreAlerts
      {
         get
         {
            lock (_alertScanGate)
            {
               return new Dictionary<string, IReadOnlyList<IAlert>>(_coreAlerts, StringComparer.Ordinal);
            }
         }
      }

      public event EventHandler<AlertsChangedEventArgs>? ActivityReviewAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? PasswordUpdateReminderAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? DuplicatedPasswordsAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? PasswordLeakedAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? VaultSecuritySettingsAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? InsufficientPasskeysAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? WeakPasskeyAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? PasskeyLeakedAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? WeakAccountPasswordAlertsChanged;
      public event EventHandler<AlertsChangedEventArgs>? PasskeyReuseAlertsChanged;
      public event EventHandler? CoreAlertsScanCompleted;

      private void _queueAlertScan()
      {
         int generation = Interlocked.Increment(ref _alertScanGeneration);
         _ = Task.Run(() => _lookAtAlertsAsync(generation));
      }

      private async Task _lookAtAlertsAsync(int generation)
      {
         if (User is null)
         {
            return;
         }

         try
         {
            IAlert[] activityAlerts = _lookAtActivityAlerts();
            IAlert[] passwordUpdateReminderAlerts = _lookAtPasswordUpdateReminderAlerts();
            (IAlert[] passwordLeakedAlerts, Account[] leakedAccounts) =
               await _lookAtPasswordLeakedAlertsAsync().ConfigureAwait(false);
            IAlert[] duplicatedPasswordsAlerts = _lookAtDuplicatedPasswordsAlerts();
            IAlert[] securitySettingsAlerts = _lookAtSecuritySettingsAlerts();
            IAlert[] insufficientPasskeysAlerts = _lookAtInsufficientPasskeysAlerts();
            IAlert[] weakPasskeyAlerts = _lookAtWeakPasskeyAlerts();
            IAlert[] passkeyLeakedAlerts =
               await _lookAtPasskeyLeakedAlertsAsync().ConfigureAwait(false);
            IAlert[] weakAccountPasswordAlerts = _lookAtWeakAccountPasswordAlerts();
            IAlert[] passkeyReuseAlerts = _lookAtPasskeyReuseAlerts();

            Dictionary<string, IReadOnlyList<IAlert>> snapshot;
            lock (_alertScanGate)
            {
               if (generation != _alertScanGeneration)
               {
                  return;
               }

               foreach (Account account in User.Services.SelectMany(static x => x.Accounts))
               {
                  account.PasswordLeaked = false;
               }

               foreach (Account account in leakedAccounts)
               {
                  account.PasswordLeaked = true;
               }

               snapshot = new Dictionary<string, IReadOnlyList<IAlert>>(StringComparer.Ordinal)
               {
                  [AlertKinds.ActivityReview] = activityAlerts,
                  [AlertKinds.PasswordUpdateReminder] = passwordUpdateReminderAlerts,
                  [AlertKinds.PasswordLeaked] = passwordLeakedAlerts,
                  [AlertKinds.DuplicatedPasswords] = duplicatedPasswordsAlerts,
                  [AlertKinds.VaultSecuritySettings] = securitySettingsAlerts,
                  [AlertKinds.InsufficientPasskeys] = insufficientPasskeysAlerts,
                  [AlertKinds.WeakPasskey] = weakPasskeyAlerts,
                  [AlertKinds.PasskeyLeaked] = passkeyLeakedAlerts,
                  [AlertKinds.WeakAccountPassword] = weakAccountPasswordAlerts,
                  [AlertKinds.PasskeyReusedAsAccountPassword] = passkeyReuseAlerts,
               };

               _coreAlerts.Clear();
               foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in snapshot)
               {
                  _coreAlerts[pair.Key] = pair.Value;
               }
            }

            _raiseKind(ActivityReviewAlertsChanged, AlertKinds.ActivityReview, snapshot);
            _raiseKind(PasswordUpdateReminderAlertsChanged, AlertKinds.PasswordUpdateReminder, snapshot);
            _raiseKind(PasswordLeakedAlertsChanged, AlertKinds.PasswordLeaked, snapshot);
            _raiseKind(DuplicatedPasswordsAlertsChanged, AlertKinds.DuplicatedPasswords, snapshot);
            _raiseKind(VaultSecuritySettingsAlertsChanged, AlertKinds.VaultSecuritySettings, snapshot);
            _raiseKind(InsufficientPasskeysAlertsChanged, AlertKinds.InsufficientPasskeys, snapshot);
            _raiseKind(WeakPasskeyAlertsChanged, AlertKinds.WeakPasskey, snapshot);
            _raiseKind(PasskeyLeakedAlertsChanged, AlertKinds.PasskeyLeaked, snapshot);
            _raiseKind(WeakAccountPasswordAlertsChanged, AlertKinds.WeakAccountPassword, snapshot);
            _raiseKind(PasskeyReuseAlertsChanged, AlertKinds.PasskeyReusedAsAccountPassword, snapshot);

            CoreAlertsScanCompleted?.Invoke(this, EventArgs.Empty);
         }
         catch (NullValueException ex)
         {
            // The alert scan runs on a background task and must never crash the
            // session; a failure only means alerts are not refreshed this round,
            // so we trace it for diagnostics rather than swallowing it silently.
            System.Diagnostics.Trace.TraceWarning($"Alert scan failed: {ex}");
         }
      }

      private void _raiseKind(
         EventHandler<AlertsChangedEventArgs>? handler,
         string kind,
         Dictionary<string, IReadOnlyList<IAlert>> snapshot)
      {
         if (handler is null)
         {
            return;
         }

         IReadOnlyList<IAlert> alerts = snapshot.TryGetValue(kind, out IReadOnlyList<IAlert>? list)
            ? list
            : [];
         handler.Invoke(this, new AlertsChangedEventArgs(kind, alerts));
      }

      private IAlert[] _lookAtActivityAlerts()
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         IActivity[] activities = ActivityCenter.GetActivitiesNeedingReview();
         return activities.Length != 0 ? [new ActivityReviewAlert(activities)] : [];
      }

      private IAlert[] _lookAtPasswordUpdateReminderAlerts()
      {
         if (User is null)
         {
            return [];
         }

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)];

         return accounts.Length != 0
            ? [new AccountsAlert(AlertKinds.PasswordUpdateReminder, AlertSeverity.Critical, accounts)]
            : [];
      }

      private const int MAX_CONCURRENT_LEAK_CHECKS = 8;

      private async Task<(IAlert[] Alerts, Account[] LeakedAccounts)> _lookAtPasswordLeakedAlertsAsync()
      {
         if (User is null)
         {
            return ([], []);
         }

         string[] passwordsToCheck = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.Options.HasFlag(AccountOption.WarnIfPasswordLeaked))
            .Select(x => x.Password)
            .Distinct()];

         HashSet<string> leakedPasswords = [];

         foreach (string[] batch in passwordsToCheck.Chunk(MAX_CONCURRENT_LEAK_CHECKS))
         {
            bool[] leaked = await Task.WhenAll(batch.Select(x => PasswordFactory.PasswordLeakedAsync(x))).ConfigureAwait(false);

            for (int i = 0; i < batch.Length; i++)
            {
               if (leaked[i])
               {
                  _ = leakedPasswords.Add(batch[i]);
               }
            }
         }

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)
               && leakedPasswords.Contains(x.Password))];

         IAlert[] alerts = accounts.Length != 0
            ? [new AccountsAlert(AlertKinds.PasswordLeaked, AlertSeverity.Critical, accounts)]
            : [];
         return (alerts, accounts);
      }

      private IAlert[] _lookAtDuplicatedPasswordsAlerts()
      {
         if (User is null)
         {
            return [];
         }

         IGrouping<string, Account>[] duplicatedPasswords = [.. User.Services
            .SelectMany(x => x.Accounts)
            .GroupBy(x => x.Password)
            .Where(x => x.Count() > 1
               && x.Any(y => y.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword)))];

         List<IAlert> alerts = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            alerts.Add(new AccountsAlert(
               AlertKinds.DuplicatedPasswords,
               AlertSeverity.Warning,
               [.. accounts]));
         }

         return [.. alerts];
      }

      private IAlert[] _lookAtSecuritySettingsAlerts()
      {
         if (User is null)
         {
            return [];
         }

         SecuritySettingsIssue issues = SecuritySettingsIssue.None;

         if (User.Settings.LogoutTimeout == 0)
         {
            issues |= SecuritySettingsIssue.AutoLogoutDisabled;
         }

         if (User.Settings.CleaningClipboardTimeout == 0)
         {
            issues |= SecuritySettingsIssue.ClipboardCleaningDisabled;
         }

         if (User.Settings.ShowPasswordDelay == 0)
         {
            issues |= SecuritySettingsIssue.QrAutoCloseDisabled;
         }

         Account[] accounts = [.. User.Services.SelectMany(static x => x.Accounts)];
         if (accounts.Length != 0)
         {
            if (!accounts.Any(static a => a.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)))
            {
               issues |= SecuritySettingsIssue.NoAccountLeakCheck;
            }

            if (!accounts.Any(static a => a.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword)))
            {
               issues |= SecuritySettingsIssue.NoAccountDuplicateCheck;
            }

            if (!accounts.Any(static a => a.PasswordUpdateReminderDelay > 0))
            {
               issues |= SecuritySettingsIssue.NoAccountUpdateReminder;
            }
         }

         return issues == SecuritySettingsIssue.None
            ? []
            : [new VaultSecuritySettingsAlert(issues)];
      }

      private IAlert[] _lookAtInsufficientPasskeysAlerts()
      {
         if (User is null)
         {
            return [];
         }

         int count = ((IUser)User).Passkeys.Count();
         return count >= AlertKinds.RecommendedPasskeyCount
            ? []
            : [
            new InsufficientPasskeysAlert(count, AlertKinds.RecommendedPasskeyCount),
         ];
      }

      private IAlert[] _lookAtWeakPasskeyAlerts()
      {
         if (User is null)
         {
            return [];
         }

         string username = User.Username;
         List<int> weakIndexes = [];
         SecretQualityIssue combined = SecretQualityIssue.None;
         int index = 0;

         foreach (string passkey in ((IUser)User).Passkeys)
         {
            SecretQualityIssue issues = SecretQuality.Evaluate(passkey, username);
            if (issues != SecretQualityIssue.None)
            {
               weakIndexes.Add(index);
               combined |= issues;
            }

            index++;
         }

         return weakIndexes.Count == 0
            ? []
            : [new WeakPasskeyAlert(weakIndexes, combined)];
      }

      private async Task<IAlert[]> _lookAtPasskeyLeakedAlertsAsync()
      {
         if (User is null)
         {
            return [];
         }

         string[] passkeys = [.. ((IUser)User).Passkeys];
         List<int> leakedIndexes = [];

         for (int offset = 0; offset < passkeys.Length; offset += MAX_CONCURRENT_LEAK_CHECKS)
         {
            int take = Math.Min(MAX_CONCURRENT_LEAK_CHECKS, passkeys.Length - offset);
            Task<bool>[] tasks = new Task<bool>[take];
            for (int i = 0; i < take; i++)
            {
               tasks[i] = PasswordFactory.PasswordLeakedAsync(passkeys[offset + i]);
            }

            bool[] leaked = await Task.WhenAll(tasks).ConfigureAwait(false);
            for (int i = 0; i < take; i++)
            {
               if (leaked[i])
               {
                  leakedIndexes.Add(offset + i);
               }
            }
         }

         return leakedIndexes.Count == 0
            ? []
            : [new PasskeyLeakedAlert(leakedIndexes)];
      }

      private IAlert[] _lookAtWeakAccountPasswordAlerts()
      {
         if (User is null)
         {
            return [];
         }

         Account[] weak = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(a => SecretQuality.IsWeak(a.Password))];

         return weak.Length == 0
            ? []
            : [new AccountsAlert(AlertKinds.WeakAccountPassword, AlertSeverity.Warning, weak)];
      }

      private IAlert[] _lookAtPasskeyReuseAlerts()
      {
         if (User is null)
         {
            return [];
         }

         HashSet<string> passkeys = new(((IUser)User).Passkeys, StringComparer.Ordinal);
         Account[] reused = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(a => passkeys.Contains(a.Password))];

         return reused.Length == 0
            ? []
            : [new AccountsAlert(
               AlertKinds.PasskeyReusedAsAccountPassword,
               AlertSeverity.Critical,
               reused)];
      }
   }
}
