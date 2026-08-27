using System.IO;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Views;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class MainViewModel : ObservableObject
   {
      public static string AppTitle => AppInfo.Title;

      public string DatabaseFile
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(DatabaseLabel));
            }
         }
      } = string.Empty;

      public string DatabaseLabel => File.Exists(DatabaseFile) ? $"Database : {Path.GetFileName(DatabaseFile)}" : "No database loaded.";

      public string CredentialsLabel
      {
         get;
         set => SetProperty(ref field, value);
      } = "Username :";

      /// <summary>
      /// Drives the progress indicator shown while the database is being opened
      /// or a passkey stretched. Those operations take about a second each, which
      /// would otherwise look like a frozen window.
      /// </summary>
      public bool IsBusy
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(MenusEnabled));
               OnPropertyChanged(nameof(CanTypePasskey));
               RelayCommand.RaiseCanExecuteChanged();
            }
         }
      }

      public string BusyMessage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      /// <summary>
      /// True from a successful <c>Open</c> until the user is fully logged in or
      /// the attempt is cancelled. While set, menus and shortcuts stay disabled
      /// so a half-open session cannot race with "New User" or "Open database".
      /// The password box itself remains usable so the next passkey can be typed.
      /// </summary>
      public bool IsAwaitingPasskeys
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(MenusEnabled));
               RelayCommand.RaiseCanExecuteChanged();
            }
         }
      }

      public bool MenusEnabled => !IsBusy && !IsAwaitingPasskeys;

      public bool CanTypePasskey => !IsBusy;

      public ICommand OpenDatabaseCommand { get; }
      public ICommand NewUserCommand { get; }
      public ICommand AppSettingsCommand { get; }
      public ICommand GeneratePasswordCommand { get; }

      public event EventHandler? DatabaseSelected;
      public event EventHandler? ResetRequested;

      public MainViewModel()
      {
         OpenDatabaseCommand = new RelayCommand(_openDatabase, () => MenusEnabled);
         NewUserCommand = new RelayCommand(_newUser, () => MenusEnabled);
         AppSettingsCommand = new RelayCommand(_appSettings, () => MenusEnabled);
         GeneratePasswordCommand = new RelayCommand(_generatePassword, () => MenusEnabled);
      }

      private void _openDatabase()
      {
         string? filename = AppServices.Dialogs.PickOpenFile(
            "Passkey user database file|*.pku",
            "Open user database file");

         if (filename is null)
         {
            return;
         }

         ResetRequested?.Invoke(this, EventArgs.Empty);
         AppServices.Session.EndSession();
         DatabaseFile = filename;
         DatabaseSelected?.Invoke(this, EventArgs.Empty);
      }

      private static void _newUser()
      {
         _ = AppServices.Dialogs.ShowDialog(new UserSettingsView());
      }

      private static void _appSettings()
      {
         _ = AppServices.Dialogs.ShowDialog(new AppSettingsView());
      }

      private static void _generatePassword()
      {
         _ = AppServices.Dialogs.ShowDialog(new PasswordGenerator());
      }
   }
}
