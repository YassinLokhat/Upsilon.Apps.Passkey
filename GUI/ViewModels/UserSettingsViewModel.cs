using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserSettingsViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      private string _username = "NewUser";
      public string Username
      {
         get => _username;
         set
         {
            if (_username != value)
            {
               _username = value;
               OnPropertyChanged(nameof(Username));
            }
         }
      }

      private int _logoutTimeout = 5;
      public int LogoutTimeout
      {
         get => _logoutTimeout;
         set
         {
            if (_logoutTimeout != value)
            {
               _logoutTimeout = value;
               OnPropertyChanged(nameof(LogoutTimeout));
            }
         }
      }

      private int _cleaningClipboardTimeout = 30;
      public int CleaningClipboardTimeout
      {
         get => _cleaningClipboardTimeout;
         set
         {
            if (_cleaningClipboardTimeout != value)
            {
               _cleaningClipboardTimeout = value;
               OnPropertyChanged(nameof(CleaningClipboardTimeout));
            }
         }
      }

      private bool _notifyLogReview = true;
      public bool NotifyLogReview
      {
         get => _notifyLogReview;
         set
         {
            if (_notifyLogReview != value)
            {
               _notifyLogReview = value;
               OnPropertyChanged(nameof(NotifyLogReview));
            }
         }
      }

      private bool _notifyPasswordUpdateReminder = true;
      public bool NotifyPasswordUpdateReminder
      {
         get => _notifyPasswordUpdateReminder;
         set
         {
            if (_notifyPasswordUpdateReminder != value)
            {
               _notifyPasswordUpdateReminder = value;
               OnPropertyChanged(nameof(NotifyPasswordUpdateReminder));
            }
         }
      }

      private bool _notifyDuplicatedPasswords = true;
      public bool NotifyDuplicatedPasswords
      {
         get => _notifyDuplicatedPasswords;
         set
         {
            if (_notifyDuplicatedPasswords != value)
            {
               _notifyDuplicatedPasswords = value;
               OnPropertyChanged(nameof(NotifyDuplicatedPasswords));
            }
         }
      }

      private bool _notifyPasswordLeaked = true;
      public bool NotifyPasswordLeaked
      {
         get => _notifyPasswordLeaked;
         set
         {
            if (_notifyPasswordLeaked != value)
            {
               _notifyPasswordLeaked = value;
               OnPropertyChanged(nameof(NotifyPasswordLeaked));
            }
         }
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
