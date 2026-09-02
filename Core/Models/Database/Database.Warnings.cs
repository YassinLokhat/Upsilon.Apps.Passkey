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
      private int _warningScanGeneration;
      private readonly object _warningScanGate = new();

      private void _queueWarningScan()
      {
         int generation = Interlocked.Increment(ref _warningScanGeneration);
         _ = Task.Run(() => _lookAtWarningsAsync(generation));
      }

      private async Task _lookAtWarningsAsync(int generation)
      {
         if (User is null)
         {
            return;
         }

         try
         {
            Warning[] activityWarnings = _lookAtActivityWarnings();
            Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            (Warning[] passwordLeakedWarnings, Account[] leakedAccounts) =
               await _lookAtPasswordLeakedWarningsAsync().ConfigureAwait(false);
            Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();
            Warning[] securitySettingsWarnings = _lookAtSecuritySettingsWarnings();

            Warning[] notified;
            lock (_warningScanGate)
            {
               if (generation != _warningScanGeneration)
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

               Warnings = [..activityWarnings,
                  ..passwordUpdateReminderWarnings,
                  ..passwordLeakedWarnings,
                  ..duplicatedPasswordsWarnings,
                  ..securitySettingsWarnings];

               // The leak check awaits a remote service, so the session may have
               // been closed in the meantime: notify against the user observed now,
               // not the one observed when the scan started.
               notified = [.. Warnings.Where(x => User.Settings.WarningsToNotify.HasFlag(x.WarningType))];
            }

            WarningsUpdated?.Invoke(this, new WarningsUpdatedEventArgs(notified));
         }
         catch (NullValueException ex)
         {
            // The warning scan runs on a background task and must never crash the
            // session; a failure only means warnings are not refreshed this round,
            // so we trace it for diagnostics rather than swallowing it silently.
            System.Diagnostics.Trace.TraceWarning($"Warning scan failed: {ex}");
         }
      }

      private Warning[] _lookAtActivityWarnings()
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         IActivity[] activities = ActivityCenter.GetActivitiesNeedingReview();

         return activities.Length != 0 ? [new Warning([.. activities])] : [];
      }

      private Warning[] _lookAtPasswordUpdateReminderWarnings()
      {
         if (User is null)
         {
            return [];
         }

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)];

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordUpdateReminderWarning, accounts)] : [];
      }

      // Leak checks are the only outbound calls the application makes, and the
      // previous parallel fan-out fired one request - and blocked one thread -
      // per distinct password at once. Requests are now awaited rather than
      // blocking, and issued in bounded batches so a large database cannot flood
      // a courtesy service.
      private const int MAX_CONCURRENT_LEAK_CHECKS = 8;

      private async Task<(Warning[] Warnings, Account[] LeakedAccounts)> _lookAtPasswordLeakedWarningsAsync()
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

         Warning[] warnings = accounts.Length != 0 ? [new Warning(WarningType.PasswordLeakedWarning, accounts)] : [];
         return (warnings, accounts);
      }

      private Warning[] _lookAtDuplicatedPasswordsWarnings()
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

         List<Warning> warnings = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            warnings.Add(new(WarningType.DuplicatedPasswordsWarning, [.. accounts.Cast<Account>()]));
         }

         return [.. warnings];
      }

      private Warning[] _lookAtSecuritySettingsWarnings()
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

         WarningType notify = User.Settings.WarningsToNotify;
         if (!notify.HasFlag(WarningType.DuplicatedPasswordsWarning))
         {
            issues |= SecuritySettingsIssue.DuplicatePasswordNotificationsDisabled;
         }

         if (!notify.HasFlag(WarningType.PasswordUpdateReminderWarning))
         {
            issues |= SecuritySettingsIssue.PasswordUpdateReminderNotificationsDisabled;
         }

         if (!notify.HasFlag(WarningType.PasswordLeakedWarning))
         {
            issues |= SecuritySettingsIssue.PasswordLeakedNotificationsDisabled;
         }

         Account[] accounts = [.. User.Services.SelectMany(static x => x.Accounts)];
         if (accounts.Length != 0)
         {
            // Per monitoring kind: warn only when zero accounts opted in.
            // One account with leak checks is enough to clear NoAccountLeakCheck
            // even if other accounts leave it off.
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

         SecuritySettingsIssue hostIssues = HostSecuritySettingsIssues?.Invoke() ?? SecuritySettingsIssue.None;
         issues |= hostIssues;

         return issues == SecuritySettingsIssue.None ? [] : [new Warning(issues)];
      }
   }
}
