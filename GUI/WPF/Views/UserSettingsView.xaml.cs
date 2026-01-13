using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserSettingsView.xaml
   /// </summary>
   public partial class UserSettingsView : Window
   {
      private readonly UserSettingsViewModel _viewModel;
      private Task? _saveTask;
      private Task? _importTask;
      private Task? _exportTask;

      public UserSettingsView()
      {
         InitializeComponent();

         _deleteUser_MI.Visibility
            = _import_MI.Visibility
            = _export_MI.Visibility
            = (MainViewModel.Database is null || MainViewModel.Database.User is null) ? Visibility.Collapsed : Visibility.Visible;

         DataContext = _viewModel = new UserSettingsViewModel();

         if (MainViewModel.Database is not null
            && MainViewModel.Database.User is not null)
         {
            MainViewModel.User.Shake();
            MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         }

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      public static void ShowUserSettings(Window owner)
      {
         _ = new UserSettingsView()
         {
            Owner = owner
         }
         .ShowDialog();
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
      {
         try
         {
            Dispatcher.Invoke(() =>
            {
               DialogResult = true;
            });
         }
         catch { }
      }

      private void _value_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         NumericTextBoxHelper.PreviewTextInput(sender, e);
      }

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         NumericTextBoxHelper.Pasting(sender, e);
      }

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         NumericTextBoxHelper.TextChanged(sender, e);
      }

      private string _canSave()
      {
         return string.IsNullOrEmpty(_viewModel.Username)
            ? "Username cannot be empty."
            : _passwordsContainer.Passkeys.Length == 0
            ? "At least one password should be set."
            : _passwordsContainer.Passkeys.Any(string.IsNullOrEmpty)
            ? "No password can be empty."
            : string.Empty;
      }

      private void _deleteUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || MainViewModel.Database is null
            || MainViewModel.Database.User is null
            || MessageBox.Show("If you delete the user database, you will lost all credentials.\nAre you sure you want to delete the database anyway?", "Confirmation required", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes
            || MessageBox.Show("This procedure is non-reversible.\nPlease confirm to proceed the deletion.", "Confirmation required", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         string databaseDirectory = Path.GetDirectoryName(MainViewModel.Database.DatabaseFile) ?? string.Empty;

         MainViewModel.Database.Delete();

         if (Directory.Exists(databaseDirectory))
         {
            Directory.Delete(databaseDirectory, true);
         }

         _ = MessageBox.Show($"'{_viewModel.Username}' user database deleted successfully", "Success");
      }

      private void _save()
      {
         string error = _canSave();
         if (!string.IsNullOrEmpty(error))
         {
            _ = MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.Invoke(() =>
            {
               this.SetIsBusy(false);
            });

            return;
         }

         string newFilename = MainViewModel.CryptographyCenter.GetHash(_viewModel.Username);
         string newDatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{newFilename}.pku");

         bool newUser = false;
         bool credentialsChanged = false;
         string oldDatabaseFile = string.Empty;

         if (MainViewModel.Database?.User is null)
         {
            try
            {
               if (MessageBox.Show($"Use default database location :\n{newDatabaseFile}", "Use default location?", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
               {
                  SaveFileDialog dialog = new()
                  {
                     Title = "New user database file",
                     Filter = "Passkey user database file|*.pku",
                     DefaultDirectory = Path.GetDirectoryName(newDatabaseFile),
                     FileName = Path.GetFileName(newDatabaseFile),
                  };

                  if (dialog.ShowDialog() ?? false)
                  {
                     newDatabaseFile = dialog.FileName;
                  }
               }

               MainViewModel.Database = Database.Create(MainViewModel.CryptographyCenter,
                  MainViewModel.SerializationCenter,
                  MainViewModel.PasswordFactory,
                  MainViewModel.ClipboardManager,
                  newDatabaseFile,
                  _viewModel.Username,
                  _passwordsContainer.Passkeys);

               MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
            }
            catch (Exception ex)
            {
               _ = MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               Dispatcher.Invoke(() =>
               {
                  this.SetIsBusy(false);
               });

               return;
            }

            newUser = true;
         }
         else
         {
            string oldFileName = MainViewModel.CryptographyCenter.GetHash(MainViewModel.Database.User.Username);
            oldDatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{oldFileName}.pku");

            credentialsChanged = _credentialsChanged(oldFileName,
               oldPasskeys: MainViewModel.Database.User.Passkeys,
               newFilename,
               newPasskeys: _passwordsContainer.Passkeys);
         }

         if (MainViewModel.Database.User is not null)
         {
            MainViewModel.Database.User.Username = _viewModel.Username;
            MainViewModel.Database.User.Passkeys = _passwordsContainer.Passkeys;
            MainViewModel.Database.User.LogoutTimeout = _viewModel.LogoutTimeout;
            MainViewModel.Database.User.CleaningClipboardTimeout = _viewModel.CleaningClipboardTimeout;
            MainViewModel.Database.User.ShowPasswordDelay = _viewModel.ShowPasswordDelay;
            MainViewModel.Database.User.NumberOfOldPasswordToKeep = _viewModel.NumberOfOldPasswordToKeep;
            MainViewModel.Database.User.NumberOfMonthActivitiesToKeep = _viewModel.NumberOfMonthActivitiesToKeep;
            WarningType warningsToNotify = 0;
            if (_viewModel.NotifyActivityReview) warningsToNotify |= WarningType.ActivityReviewWarning;
            if (_viewModel.NotifyDuplicatedPasswords) warningsToNotify |= WarningType.DuplicatedPasswordsWarning;
            if (_viewModel.NotifyPasswordUpdateReminder) warningsToNotify |= WarningType.PasswordUpdateReminderWarning;
            if (_viewModel.NotifyPasswordLeaked) warningsToNotify |= WarningType.PasswordLeakedWarning;
            MainViewModel.Database.User.WarningsToNotify = warningsToNotify;

            MainViewModel.Database.Save();
         }

         string message = $"'{_viewModel.Username}' user database ";

         if (credentialsChanged)
         {
            message = $"'{_viewModel.Username}' user's credentials has been updated.\nYou will be logged out.\nPlease login again.";
            MainViewModel.Database.Close();

            string oldDatabaseDirectory = Path.GetDirectoryName(oldDatabaseFile) ?? string.Empty;
            string newDatabaseDirectory = Path.GetDirectoryName(newDatabaseFile) ?? string.Empty;

            if (oldDatabaseDirectory != newDatabaseDirectory)
            {
               if (!Directory.Exists(newDatabaseDirectory))
               {
                  _ = Directory.CreateDirectory(newDatabaseDirectory);
               }

               if (File.Exists(oldDatabaseFile))
               {
                  File.Move(oldDatabaseFile, newDatabaseFile);
               }

               if (Directory.Exists(oldDatabaseDirectory))
               {
                  Directory.Delete(oldDatabaseDirectory, true);
               }
            }
         }
         else if (newUser)
         {
            message += $"created successfully";
            MainViewModel.Database.Close();
         }
         else
         {
            message += $"updated successfully";
            Dispatcher.Invoke(() =>
            {
               DialogResult = true;
            });
         }

         _ = MessageBox.Show(message, "Success");
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         this.SetIsBusy(true);

         if (_saveTask is null
            || _saveTask.IsCompleted)
         {
            _saveTask = Task.Run(_save);
         }
      }

      private static bool _credentialsChanged(string oldFileName, string[] oldPasskeys, string newFilename, string[] newPasskeys)
      {
         return oldFileName != newFilename || MainViewModel.SerializationCenter.AreDifferent(oldPasskeys, newPasskeys);
      }

      private void _import_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || MainViewModel.Database?.User is null)
         {
            return;
         }

         if (MainViewModel.Database.User.HasChanged()
            && MessageBox.Show("Before importing data, all unsaved changes will be saved.", "Import data", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
         {
            return;
         }

         OpenFileDialog dialog = new()
         {
            Title = "Import data from a file",
            Filter = "Tab delimited CSV file|*.csv|json file|*.json",
         };

         if (!(dialog.ShowDialog() ?? false)) return;

         this.SetIsBusy(true);

         if (_importTask is null
            || _importTask.IsCompleted)
         {
            _importTask = Task.Run(() =>
            {
               _ = MainViewModel.Database.ImportFromFile(dialog.FileName)
                  ? MessageBox.Show("Import data has been completed successfully.\nMore details in the activities.", "Import success")
                  : MessageBox.Show("Import data failed.\nMore details in the activities.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);

               Dispatcher.Invoke(() =>
               {
                  this.SetIsBusy(false);
               });
            });
         }
      }

      private void _export_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || MainViewModel.Database?.User is null)
         {
            return;
         }

         if (MainViewModel.Database.User.HasChanged()
            && MessageBox.Show("Before exporting data, all unsaved changes will be saved.", "Export data", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = "Export data to a file",
            Filter = "Tab delimited CSV file|*.csv|json file|*.json",
            FileName = $"{MainViewModel.Database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}",
         };

         if (!(dialog.ShowDialog() ?? false)) return;

         this.SetIsBusy(true);

         if (_exportTask is null
            || _exportTask.IsCompleted)
         {
            _exportTask = Task.Run(() =>
            {
               _ = MainViewModel.Database.ExportToFile(dialog.FileName)
                  ? MessageBox.Show("Export data has been completed successfully.\nMore details in the activities.", "Export success")
                  : MessageBox.Show("Export data failed.\nMore details in the activities.", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);

               Dispatcher.Invoke(() =>
               {
                  this.SetIsBusy(false);
               });
            });
         }
      }
   }
}
