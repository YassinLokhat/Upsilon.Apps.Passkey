using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      private async Task _lookAtWarningsAsync()
      {
         if (User is null)
         {
            return;
         }

         try
         {
            Warning[] activityWarnings = _lookAtActivityWarnings();
            Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            Warning[] passwordLeakedWarnings = await _lookAtPasswordLeakedWarningsAsync().ConfigureAwait(false);
            Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();

            Warnings = [..activityWarnings,
               ..passwordUpdateReminderWarnings,
               ..passwordLeakedWarnings,
               ..duplicatedPasswordsWarnings];

            // The leak check awaits a remote service, so the session may have
            // been closed in the meantime: notify against the user observed now,
            // not the one observed when the scan started.
            WarningsUpdated?.Invoke(this, new WarningsUpdatedEventArgs([.. Warnings.Where(x => User.Settings.WarningsToNotify.HasFlag(x.WarningType))]));
         }
#pragma warning disable CA1031 // Last-resort barrier: the background warning scan must never crash the session
         catch (Exception ex)
#pragma warning restore CA1031
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

      private async Task<Warning[]> _lookAtPasswordLeakedWarningsAsync()
      {
         if (User is null)
         {
            return [];
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

         foreach (Account account in accounts)
         {
            account.PasswordLeaked = true;
         }

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordLeakedWarning, accounts)] : [];
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
            .Where(x => x.Count() > 1)];

         List<Warning> warnings = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            if (accounts.Any(x => x.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword)))
            {
               warnings.Add(new(WarningType.DuplicatedPasswordsWarning, [.. accounts.Cast<Account>()]));
            }
         }

         return [.. warnings];
      }
   }
}
