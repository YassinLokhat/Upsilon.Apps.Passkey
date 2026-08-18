using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Settings : ISettings
   {
      #region ISettings interface explicit Internal

      int ISettings.LogoutTimeout
      {
         get => User.Database.Get(LogoutTimeout);
         set => LogoutTimeout = User.Database.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(LogoutTimeout),
            needsReview: false,
            oldValue: LogoutTimeout,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.CleaningClipboardTimeout
      {
         get => User.Database.Get(CleaningClipboardTimeout);
         set => CleaningClipboardTimeout = User.Database.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(CleaningClipboardTimeout),
            needsReview: false,
            oldValue: CleaningClipboardTimeout,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.ShowPasswordDelay
      {
         get => User.Database.Get(ShowPasswordDelay);
         set => ShowPasswordDelay = User.Database.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(ShowPasswordDelay),
            needsReview: false,
            oldValue: ShowPasswordDelay,
            newValue: value,
            readableValue: $"{value}");
      }

      int ISettings.NumberOfOldPasswordToKeep
      {
         get => User.Database.Get(NumberOfOldPasswordToKeep);
         set
         {
            NumberOfOldPasswordToKeep = User.Database.AutoSave.UpdateValue(User.ItemId,
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
         get => User.Database.Get(NumberOfMonthActivitiesToKeep);
         set
         {
            NumberOfMonthActivitiesToKeep = User.Database.AutoSave.UpdateValue(User.ItemId,
               fieldName: nameof(NumberOfMonthActivitiesToKeep),
               needsReview: true,
               oldValue: NumberOfMonthActivitiesToKeep,
               newValue: value,
               readableValue: $"{value}");

            User.Database.ActivityCenter.Save(rebuildStringActivities: true);
         }
      }

      WarningType ISettings.WarningsToNotify
      {
         get => User.Database.Get(WarningsToNotify);
         set => WarningsToNotify = User.Database.AutoSave.UpdateValue(User.ItemId,
            fieldName: nameof(WarningsToNotify),
            needsReview: true,
            oldValue: WarningsToNotify,
            newValue: value,
            readableValue: value.ToString());
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
   }
}
