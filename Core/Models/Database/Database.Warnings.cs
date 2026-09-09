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

      private readonly Dictionary<string, IReadOnlyList<IWarning>> _coreWarnings = new(StringComparer.Ordinal);

      public IReadOnlyDictionary<string, IReadOnlyList<IWarning>> CoreWarnings
      {
         get
         {
            lock (_warningScanGate)
            {
               return new Dictionary<string, IReadOnlyList<IWarning>>(_coreWarnings, StringComparer.Ordinal);
            }
         }
      }

      public event EventHandler<WarningsChangedEventArgs>? ActivityReviewWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? PasswordUpdateReminderWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? DuplicatedPasswordsWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? PasswordLeakedWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? VaultSecuritySettingsWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? InsufficientPasskeysWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? WeakPasskeyWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? PasskeyLeakedWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? WeakAccountPasswordWarningsChanged;
      public event EventHandler<WarningsChangedEventArgs>? PasskeyReuseWarningsChanged;
      public event EventHandler? CoreWarningsScanCompleted;

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
            IWarning[] activityWarnings = _lookAtActivityWarnings();
            IWarning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            (IWarning[] passwordLeakedWarnings, Account[] leakedAccounts) =
               await _lookAtPasswordLeakedWarningsAsync().ConfigureAwait(false);
            IWarning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();
            IWarning[] securitySettingsWarnings = _lookAtSecuritySettingsWarnings();
            IWarning[] insufficientPasskeysWarnings = _lookAtInsufficientPasskeysWarnings();
            IWarning[] weakPasskeyWarnings = _lookAtWeakPasskeyWarnings();
            IWarning[] passkeyLeakedWarnings =
               await _lookAtPasskeyLeakedWarningsAsync().ConfigureAwait(false);
            IWarning[] weakAccountPasswordWarnings = _lookAtWeakAccountPasswordWarnings();
            IWarning[] passkeyReuseWarnings = _lookAtPasskeyReuseWarnings();

            Dictionary<string, IReadOnlyList<IWarning>> snapshot;
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

               snapshot = new Dictionary<string, IReadOnlyList<IWarning>>(StringComparer.Ordinal)
               {
                  [WarningKinds.ActivityReview] = activityWarnings,
                  [WarningKinds.PasswordUpdateReminder] = passwordUpdateReminderWarnings,
                  [WarningKinds.PasswordLeaked] = passwordLeakedWarnings,
                  [WarningKinds.DuplicatedPasswords] = duplicatedPasswordsWarnings,
                  [WarningKinds.VaultSecuritySettings] = securitySettingsWarnings,
                  [WarningKinds.InsufficientPasskeys] = insufficientPasskeysWarnings,
                  [WarningKinds.WeakPasskey] = weakPasskeyWarnings,
                  [WarningKinds.PasskeyLeaked] = passkeyLeakedWarnings,
                  [WarningKinds.WeakAccountPassword] = weakAccountPasswordWarnings,
                  [WarningKinds.PasskeyReusedAsAccountPassword] = passkeyReuseWarnings,
               };

               _coreWarnings.Clear();
               foreach (KeyValuePair<string, IReadOnlyList<IWarning>> pair in snapshot)
               {
                  _coreWarnings[pair.Key] = pair.Value;
               }
            }

            _raiseKind(ActivityReviewWarningsChanged, WarningKinds.ActivityReview, snapshot);
            _raiseKind(PasswordUpdateReminderWarningsChanged, WarningKinds.PasswordUpdateReminder, snapshot);
            _raiseKind(PasswordLeakedWarningsChanged, WarningKinds.PasswordLeaked, snapshot);
            _raiseKind(DuplicatedPasswordsWarningsChanged, WarningKinds.DuplicatedPasswords, snapshot);
            _raiseKind(VaultSecuritySettingsWarningsChanged, WarningKinds.VaultSecuritySettings, snapshot);
            _raiseKind(InsufficientPasskeysWarningsChanged, WarningKinds.InsufficientPasskeys, snapshot);
            _raiseKind(WeakPasskeyWarningsChanged, WarningKinds.WeakPasskey, snapshot);
            _raiseKind(PasskeyLeakedWarningsChanged, WarningKinds.PasskeyLeaked, snapshot);
            _raiseKind(WeakAccountPasswordWarningsChanged, WarningKinds.WeakAccountPassword, snapshot);
            _raiseKind(PasskeyReuseWarningsChanged, WarningKinds.PasskeyReusedAsAccountPassword, snapshot);

            CoreWarningsScanCompleted?.Invoke(this, EventArgs.Empty);
         }
         catch (NullValueException ex)
         {
            // The warning scan runs on a background task and must never crash the
            // session; a failure only means warnings are not refreshed this round,
            // so we trace it for diagnostics rather than swallowing it silently.
            System.Diagnostics.Trace.TraceWarning($"Warning scan failed: {ex}");
         }
      }

      private void _raiseKind(
         EventHandler<WarningsChangedEventArgs>? handler,
         string kind,
         Dictionary<string, IReadOnlyList<IWarning>> snapshot)
      {
         if (handler is null)
         {
            return;
         }

         IReadOnlyList<IWarning> warnings = snapshot.TryGetValue(kind, out IReadOnlyList<IWarning>? list)
            ? list
            : [];
         handler.Invoke(this, new WarningsChangedEventArgs(kind, warnings));
      }

      private IWarning[] _lookAtActivityWarnings()
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         IActivity[] activities = ActivityCenter.GetActivitiesNeedingReview();
         return activities.Length != 0 ? [new ActivityReviewWarning(activities)] : [];
      }

      private IWarning[] _lookAtPasswordUpdateReminderWarnings()
      {
         if (User is null)
         {
            return [];
         }

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)];

         return accounts.Length != 0
            ? [new AccountsWarning(WarningKinds.PasswordUpdateReminder, WarningSeverity.Critical, accounts)]
            : [];
      }

      private const int MAX_CONCURRENT_LEAK_CHECKS = 8;

      private async Task<(IWarning[] Warnings, Account[] LeakedAccounts)> _lookAtPasswordLeakedWarningsAsync()
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

         IWarning[] warnings = accounts.Length != 0
            ? [new AccountsWarning(WarningKinds.PasswordLeaked, WarningSeverity.Critical, accounts)]
            : [];
         return (warnings, accounts);
      }

      private IWarning[] _lookAtDuplicatedPasswordsWarnings()
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

         List<IWarning> warnings = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            warnings.Add(new AccountsWarning(
               WarningKinds.DuplicatedPasswords,
               WarningSeverity.Warning,
               [.. accounts]));
         }

         return [.. warnings];
      }

      private IWarning[] _lookAtSecuritySettingsWarnings()
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
            : [new VaultSecuritySettingsWarning(issues)];
      }

      private IWarning[] _lookAtInsufficientPasskeysWarnings()
      {
         if (User is null)
         {
            return [];
         }

         int count = ((IUser)User).Passkeys.Count();
         if (count >= WarningKinds.RecommendedPasskeyCount)
         {
            return [];
         }

         return
         [
            new InsufficientPasskeysWarning(count, WarningKinds.RecommendedPasskeyCount),
         ];
      }

      private IWarning[] _lookAtWeakPasskeyWarnings()
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
            : [new WeakPasskeyWarning(weakIndexes, combined)];
      }

      private async Task<IWarning[]> _lookAtPasskeyLeakedWarningsAsync()
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
            : [new PasskeyLeakedWarning(leakedIndexes)];
      }

      private IWarning[] _lookAtWeakAccountPasswordWarnings()
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
            : [new AccountsWarning(WarningKinds.WeakAccountPassword, WarningSeverity.Warning, weak)];
      }

      private IWarning[] _lookAtPasskeyReuseWarnings()
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
            : [new AccountsWarning(
               WarningKinds.PasskeyReusedAsAccountPassword,
               WarningSeverity.Critical,
               reused)];
      }
   }
}
