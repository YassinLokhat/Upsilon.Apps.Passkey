using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserSettingsViewModel : INotifyPropertyChanged
   {
      public string Title { get; }
      public string Username
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = "NewUser";
      public int LogoutTimeout
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = 5;
      public int CleaningClipboardTimeout
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = 30;
      public bool NotifyLogReview
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyPasswordUpdateReminder
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyDuplicatedPasswords
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;
      public bool NotifyPasswordLeaked
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserSettingsViewModel()
      {
         Title = MainViewModel.AppTitle;

         if (MainViewModel.Database is null || MainViewModel.Database.User is null)
         {
            Title += " - New user";
         }
         else
         {
            Title += " - User settings";

            Username = MainViewModel.Database.User.Username;

            LogoutTimeout = MainViewModel.Database.User.LogoutTimeout;
            CleaningClipboardTimeout = MainViewModel.Database.User.CleaningClipboardTimeout;

            NotifyLogReview = (MainViewModel.Database.User.WarningsToNotify & PassKey.Core.Public.Enums.WarningType.LogReviewWarning) != 0;
            NotifyPasswordUpdateReminder = (MainViewModel.Database.User.WarningsToNotify & PassKey.Core.Public.Enums.WarningType.PasswordUpdateReminderWarning) != 0;
            NotifyDuplicatedPasswords = (MainViewModel.Database.User.WarningsToNotify & PassKey.Core.Public.Enums.WarningType.DuplicatedPasswordsWarning) != 0;
            NotifyPasswordLeaked = (MainViewModel.Database.User.WarningsToNotify & PassKey.Core.Public.Enums.WarningType.PasswordLeakedWarning) != 0;
         }
      }
   }
}
