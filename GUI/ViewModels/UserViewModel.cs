using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserViewModel : INotifyPropertyChanged
   {
      private readonly string _appTitle;
      public string AppTitle => _appTitle;

      private string _username = string.Empty;
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

      private int _logoutTimeout = 0;
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

      private int _cleaningClipboardTimeout = 0;
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

      public UserViewModel(bool newUser)
      {
         _appTitle = MainViewModel.AppTitle;

         if (newUser)
         {
            _appTitle += " - New user";
         }
         else
         {
            _appTitle += " - User settings";
         }
      }
   }
}
