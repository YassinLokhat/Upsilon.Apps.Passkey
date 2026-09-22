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
      private readonly Lock _alertScanGate = new();

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

      public event EventHandler<AlertsChangedEventArgs>? CoreAlertsChanged;
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

         // True once this generation has published kinds+ScanCompleted together.
         // If we fail before any publish, finally still signals completion so the
         // UI is never left waiting on a scan that died quietly.
         bool published = false;
         try
         {
            // Phase 1 (local): publish without waiting on leak I/O so the menu
            // can show duplicates / vault / activity immediately after login.
            Dictionary<string, IReadOnlyList<IAlert>> localSnapshot = new(StringComparer.Ordinal)
            {
               [AlertKinds.ActivityReview] = _lookAtActivityAlerts(),
               [AlertKinds.PasswordUpdateReminder] = _lookAtPasswordUpdateReminderAlerts(),
               [AlertKinds.DuplicatedPasswords] = _lookAtDuplicatedPasswordsAlerts(),
               [AlertKinds.VaultSecuritySettings] = _lookAtSecuritySettingsAlerts(),
               [AlertKinds.InsufficientPasskeys] = _lookAtInsufficientPasskeysAlerts(),
               [AlertKinds.WeakPasskey] = _lookAtWeakPasskeyAlerts(),
               [AlertKinds.WeakAccountPassword] = _lookAtWeakAccountPasswordAlerts(),
               [AlertKinds.PasskeyReusedAsAccountPassword] = _lookAtPasskeyReuseAlerts(),
            };

            if (!_tryCommitAndPublish(generation, localSnapshot))
            {
               return;
            }

            published = true;

            // Phase 2 (network / local filter): patch leak kinds only; keep
            // prior leak entries visible until this phase commits.
            (IAlert[] passwordLeakedAlerts, Account[] leakedAccounts) =
               await _lookAtPasswordLeakedAlertsAsync().ConfigureAwait(false);
            IAlert[] passkeyLeakedAlerts =
               await _lookAtPasskeyLeakedAlertsAsync().ConfigureAwait(false);

            User? user = User;
            if (user is null)
            {
               return;
            }

            Dictionary<string, IReadOnlyList<IAlert>> leakSnapshot = new(StringComparer.Ordinal)
            {
               [AlertKinds.PasswordLeaked] = passwordLeakedAlerts,
               [AlertKinds.PasskeyLeaked] = passkeyLeakedAlerts,
            };

            if (_tryCommitAndPublish(generation, leakSnapshot, () =>
                {
                   foreach (Account account in user.Services.SelectMany(static x => x.Accounts))
                   {
                      account.PasswordLeaked = false;
                   }

                   foreach (Account account in leakedAccounts)
                   {
                      account.PasswordLeaked = true;
                   }
                }))
            {
               published = true;
            }
         }
#pragma warning disable CA1031 // Alert scan must not tear down the session
         catch (Exception ex)
         {
            System.Diagnostics.Trace.TraceWarning($"Alert scan failed: {ex}");
         }
#pragma warning restore CA1031
         finally
         {
            if (!published
               && generation == Volatile.Read(ref _alertScanGeneration)
               && User is not null)
            {
               CoreAlertsScanCompleted?.Invoke(this, EventArgs.Empty);
            }
         }
      }

      /// <summary>
      /// Commits <paramref name="patch"/> into <see cref="_coreAlerts"/> when
      /// <paramref name="generation"/> is still current, then raises kind events
      /// and <see cref="CoreAlertsScanCompleted"/> as one unit (no abort between
      /// them). Returns false if a newer scan already superseded this generation.
      /// </summary>
      private bool _tryCommitAndPublish(
         int generation,
         Dictionary<string, IReadOnlyList<IAlert>> patch,
         Action? mutateAccountsUnderLock = null)
      {
         Dictionary<string, IReadOnlyList<IAlert>> toPublish;
         lock (_alertScanGate)
         {
            if (generation != _alertScanGeneration)
            {
               return false;
            }

            mutateAccountsUnderLock?.Invoke();

            foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in patch)
            {
               _coreAlerts[pair.Key] = pair.Value;
            }

            toPublish = new Dictionary<string, IReadOnlyList<IAlert>>(patch, StringComparer.Ordinal);
         }

         // Discard before any event if superseded after the write; otherwise a
         // stale snapshot could overwrite a newer publish in the broker.
         if (generation != Volatile.Read(ref _alertScanGeneration))
         {
            return false;
         }

         foreach (KeyValuePair<string, IReadOnlyList<IAlert>> pair in toPublish)
         {
            CoreAlertsChanged?.Invoke(this, new AlertsChangedEventArgs(pair.Key, pair.Value));
         }

         // Always paired with the kind batch above — never return between them.
         CoreAlertsScanCompleted?.Invoke(this, EventArgs.Empty);
         return true;
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

            if (!accounts.Any(static a => a.Options.HasFlag(AccountOption.WarnIfWeakPassword)))
            {
               issues |= SecuritySettingsIssue.NoAccountWeakPasswordCheck;
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
            .Where(a => a.Options.HasFlag(AccountOption.WarnIfWeakPassword)
               && SecretQuality.IsWeak(a.Password))];

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
