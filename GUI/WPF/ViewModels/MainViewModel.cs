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

      public ICommand OpenDatabaseCommand { get; }
      public ICommand NewUserCommand { get; }
      public ICommand GeneratePasswordCommand { get; }

      public event EventHandler? DatabaseSelected;
      public event EventHandler? ResetRequested;

      public MainViewModel()
      {
         OpenDatabaseCommand = new RelayCommand(_openDatabase);
         NewUserCommand = new RelayCommand(_newUser);
         GeneratePasswordCommand = new RelayCommand(_generatePassword);
      }

      private void _openDatabase()
      {
         string? filename = AppServices.Dialogs.PickOpenFile(
            "Passkey user database file|*.pku",
            "Open user database file");

         if (filename is null) return;

         ResetRequested?.Invoke(this, EventArgs.Empty);
         AppServices.Session.EndSession();
         DatabaseFile = filename;
         DatabaseSelected?.Invoke(this, EventArgs.Empty);
      }

      private static void _newUser()
      {
         _ = AppServices.Dialogs.ShowDialog(new UserSettingsView());
      }

      private static void _generatePassword()
      {
         _ = AppServices.Dialogs.ShowDialog(new PasswordGenerator());
      }
   }
}
