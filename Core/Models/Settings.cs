using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Per-user timeouts and which <see cref="WarningType"/>s to surface.
   /// Stored on the user; edits go through autosave.
   /// </summary>
   internal sealed class Settings : ISettings
   {
      #region ISettings interface explicit Internal

      int ISettings.LogoutTimeout
      {
         get => User.Host.Touch(LogoutTimeout);
         set => LogoutTimeout = User.Host.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(LogoutTimeout),
            needsReview: false,
            oldValue: LogoutTimeout,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.CleaningClipboardTimeout
      {
         get => User.Host.Touch(CleaningClipboardTimeout);
         set => CleaningClipboardTimeout = User.Host.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(CleaningClipboardTimeout),
            needsReview: false,
            oldValue: CleaningClipboardTimeout,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.ShowPasswordDelay
      {
         get => User.Host.Touch(ShowPasswordDelay);
         set => ShowPasswordDelay = User.Host.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(ShowPasswordDelay),
            needsReview: false,
            oldValue: ShowPasswordDelay,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.NumberOfOldPasswordToKeep
      {
         get => User.Host.Touch(NumberOfOldPasswordToKeep);
         set
         {
            NumberOfOldPasswordToKeep = User.Host.AutoSave.UpdateValue(User.ItemId,
               fieldName: nameof(NumberOfOldPasswordToKeep),
               needsReview: true,
               oldValue: NumberOfOldPasswordToKeep,
               newValue: value,
               readableValue: $"{value}");

            if (NumberOfOldPasswordToKeep == 0)
            {
               return;
            }

            IEnumerable<Account> accounts = [.. User.Services.SelectMany(x => x.Accounts).Where(x => x.Passwords.Count > NumberOfOldPasswordToKeep)];

            foreach (Account account in accounts)
            {
               IEnumerable<DateTime> datesToRemove = [.. account.Passwords.Keys
                  .OrderBy(x => x)
                  .Take(account.Passwords.Count - NumberOfOldPasswordToKeep)];

               foreach (DateTime dateToRemove in datesToRemove)
               {
                  _ = account.Passwords.Remove(dateToRemove);
               }
            }
         }
      }

      int ISettings.NumberOfMonthActivitiesToKeep
      {
         get => User.Host.Touch(NumberOfMonthActivitiesToKeep);
         set
         {
            NumberOfMonthActivitiesToKeep = User.Host.AutoSave.UpdateValue(User.ItemId,
               fieldName: nameof(NumberOfMonthActivitiesToKeep),
               needsReview: true,
               oldValue: NumberOfMonthActivitiesToKeep,
               newValue: value,
               readableValue: $"{value}");

            User.Host.PersistActivityLog(rebuildStringActivities: true);
         }
      }

      WarningType ISettings.WarningsToNotify
      {
         get => User.Host.Touch(WarningsToNotify);
         set => WarningsToNotify = User.Host.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(WarningsToNotify),
            needsReview: true,
            oldValue: WarningsToNotify,
            newValue: value,
            readableValue: value.ToString());
      }

      string ISettings.Language
      {
         get => User.Host.Touch(Language);
         set => Language = User.Host.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(Language),
            needsReview: false,
            oldValue: Language,
            newValue: value ?? string.Empty,
            readableValue: string.IsNullOrEmpty(value) ? "(app)" : value);
      }

      #endregion

      internal User User
      {
         get => field ?? throw new NullValueException(nameof(User));
         set;
      }

      public int LogoutTimeout { get; set; }
      public int CleaningClipboardTimeout { get; set; }
      public int ShowPasswordDelay { get; set; }
      public int NumberOfOldPasswordToKeep { get; set; }
      public int NumberOfMonthActivitiesToKeep { get; set; }
      public WarningType WarningsToNotify { get; set; }
         = WarningType.ActivityReviewWarning
         | WarningType.PasswordUpdateReminderWarning
         | WarningType.DuplicatedPasswordsWarning
         | WarningType.PasswordLeakedWarning;

      /// <summary>Empty = use application <c>config.json</c> language.</summary>
      public string Language { get; set; } = string.Empty;
   }
}
