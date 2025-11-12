using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserSettingsViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      private string _username = "NewUser";
      public string Username
      {
         get => _username;
         set => PropertyHelper.SetProperty(ref _username, value, this, PropertyChanged);
      }

      private int _logoutTimeout = 5;
      public int LogoutTimeout
      {
         get => _logoutTimeout;
         set => PropertyHelper.SetProperty(ref _logoutTimeout, value, this, PropertyChanged);
      }

      private int _cleaningClipboardTimeout = 30;
      public int CleaningClipboardTimeout
      {
         get => _cleaningClipboardTimeout;
         set => PropertyHelper.SetProperty(ref _cleaningClipboardTimeout, value, this, PropertyChanged);
      }

      private bool _notifyLogReview = true;
      public bool NotifyLogReview
      {
         get => _notifyLogReview;
         set => PropertyHelper.SetProperty(ref _notifyLogReview, value, this, PropertyChanged);
      }

      private bool _notifyPasswordUpdateReminder = true;
      public bool NotifyPasswordUpdateReminder
      {
         get => _notifyPasswordUpdateReminder;
         set => PropertyHelper.SetProperty(ref _notifyPasswordUpdateReminder, value, this, PropertyChanged);
      }

      private bool _notifyDuplicatedPasswords = true;
      public bool NotifyDuplicatedPasswords
      {
         get => _notifyDuplicatedPasswords;
         set => PropertyHelper.SetProperty(ref _notifyDuplicatedPasswords, value, this, PropertyChanged);
      }

      private bool _notifyPasswordLeaked = true;
      public bool NotifyPasswordLeaked
      {
         get => _notifyPasswordLeaked;
         set => PropertyHelper.SetProperty(ref _notifyPasswordLeaked, value, this, PropertyChanged);
      }

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
