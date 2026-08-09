using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserSettingsView.xaml
   /// </summary>
   internal sealed partial class UserSettingsView : Window
   {
      private readonly UserSettingsViewModel _viewModel;
      private bool _isClosing;
      private IDatabase? _database;

      private static ISessionService _session => AppServices.Session;

      public UserSettingsView()
      {
         InitializeComponent();

         _database = _session.Database;
         bool hasUser = _database?.User is not null;

         _deleteUser_MI.Visibility
            = _import_MI.Visibility
            = _export_MI.Visibility
            = hasUser ? Visibility.Visible : Visibility.Collapsed;

         DataContext = _viewModel = new UserSettingsViewModel();

         if (_database is not null && _database.User is not null)
         {
            _database.User.Shake();
            _database.DatabaseClosed += _database_DatabaseClosed;
         }

         _username_TB.SelectedText = _viewModel.Username;
         _ = _username_TB.Focus();

         Loaded += (s, e) => this.PostLoadSetup();
         Closed += _window_Closed;
      }

      private void _window_Closed(object? sender, EventArgs e)
      {
         _isClosing = true;

         _database?.DatabaseClosed -= _database_DatabaseClosed;
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
          => this.DatabaseClosed(_isClosing);

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
            : !_passwordsContainer.Passkeys.Any()
            ? "At least one password should be set."
            : _passwordsContainer.Passkeys.Any(string.IsNullOrEmpty)
            ? "No password can be empty."
            : string.Empty;
      }

      private void _deleteUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _database?.User is null
            || MessageBox.Show("If you delete the user database, you will lost all credentials.\nAre you sure you want to delete the database anyway?", "Confirmation required", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes
            || MessageBox.Show("This procedure is non-reversible.\nPlease confirm to proceed the deletion.", "Confirmation required", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         _ = Path.GetDirectoryName(_database.DatabaseFile) ?? string.Empty;

         _database.Delete();

         _ = MessageBox.Show($"'{_viewModel.Username}' user database deleted successfully", "Success");
      }

      private async Task _saveAsync()
      {
         string error = _canSave();
         if (!string.IsNullOrEmpty(error))
         {
            _ = MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return;
         }

         string newFilename = AppServices.Cryptography.GetHash(_viewModel.Username);
         string newDatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{newFilename}.pku");

         bool newUser = false;
         bool credentialsChanged = false;
         string oldDatabaseFile = string.Empty;

         if (_database?.User is null)
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

               _database = await Database.CreateAsync(AppServices.Cryptography,
                  AppServices.Serialization,
                  AppServices.PasswordFactory,
                  AppServices.Clipboard,
                  newDatabaseFile,
                  _viewModel.Username,
                  [.. _passwordsContainer.Passkeys]).ConfigureAwait(true);

               _database.DatabaseClosed += _database_DatabaseClosed;
               _session.StartSession(_database);
            }
#pragma warning disable CA1031 // Last-resort barrier: database creation errors are shown to the user, not propagated
            catch (Exception ex)
#pragma warning restore CA1031
            {
               _ = MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

               return;
            }

            newUser = true;
         }
         else
         {
            string oldFileName = AppServices.Cryptography.GetHash(_database.User.Username);
            oldDatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{oldFileName}.pku");

            credentialsChanged = _credentialsChanged(oldFileName,
               oldPasskeys: _database.User.Passkeys,
               newFilename,
               newPasskeys: _passwordsContainer.Passkeys);
         }

         if (_database.User is not null)
         {
            _database.User.Username = _viewModel.Username;
            _database.User.Passkeys = _passwordsContainer.Passkeys;
            _database.User.Settings.LogoutTimeout = _viewModel.LogoutTimeout;
            _database.User.Settings.CleaningClipboardTimeout = _viewModel.CleaningClipboardTimeout;
            _database.User.Settings.ShowPasswordDelay = _viewModel.ShowPasswordDelay;
            _database.User.Settings.NumberOfOldPasswordToKeep = _viewModel.NumberOfOldPasswordToKeep;
            _database.User.Settings.NumberOfMonthActivitiesToKeep = _viewModel.NumberOfMonthActivitiesToKeep;
            WarningType warningsToNotify = 0;
            if (_viewModel.NotifyActivityReview) warningsToNotify |= WarningType.ActivityReviewWarning;
            if (_viewModel.NotifyDuplicatedPasswords) warningsToNotify |= WarningType.DuplicatedPasswordsWarning;
            if (_viewModel.NotifyPasswordUpdateReminder) warningsToNotify |= WarningType.PasswordUpdateReminderWarning;
            if (_viewModel.NotifyPasswordLeaked) warningsToNotify |= WarningType.PasswordLeakedWarning;
            _database.User.Settings.WarningsToNotify = warningsToNotify;

            await _database.SaveAsync().ConfigureAwait(true);
         }

         string message = $"'{_viewModel.Username}' user database ";

         if (credentialsChanged)
         {
            message = $"'{_viewModel.Username}' user's credentials has been updated.\nYou will be logged out.\nPlease login again.";
            _database.Close();

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
            _database.Close();
         }
         else
         {
            message += $"updated successfully";
            this.DatabaseClosed(_isClosing);
         }

         _ = MessageBox.Show(message, "Success");
      }

      private async void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         // The busy cursor is set synchronously before the first await, so it
         // doubles as the re-entrancy guard against a second save being started
         // while this one is still running.
         if (this.GetIsBusy()) return;

         this.SetIsBusy(true);

         try
         {
            await _saveAsync().ConfigureAwait(true);
         }
#pragma warning disable CA1031 // Last-resort barrier: nothing may escape an async void handler
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Failed to save the user settings");
            _ = MessageBox.Show("An unexpected error occurred while saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
         }
         finally
         {
            this.SetIsBusy(false);
         }
      }

      private static bool _credentialsChanged(string oldFileName, IEnumerable<string> oldPasskeys, string newFilename, IEnumerable<string> newPasskeys)
      {
         return oldFileName != newFilename || AppServices.Serialization.AreDifferent(oldPasskeys, newPasskeys);
      }

      private async void _import_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IDatabase? database = _database;

         if (this.GetIsBusy()
            || database?.User is null)
         {
            return;
         }

         if (database.User.HasChanged()
            && MessageBox.Show("Before importing data, all unsaved changes will be saved.", "Import data", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
         {
            return;
         }

         OpenFileDialog dialog = new()
         {
            Title = "Import data from a file",
            Filter = "json file|*.json|Tab delimited CSV file|*.csv",
         };

         if (!(dialog.ShowDialog() ?? false)) return;

         this.SetIsBusy(true);

         try
         {
            bool imported = await database.ImportFromFileAsync(dialog.FileName).ConfigureAwait(true);

            _ = imported
               ? MessageBox.Show("Import data has been completed successfully.\nMore details in the activities.", "Import success")
               : MessageBox.Show("Import data failed.\nMore details in the activities.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
         }
#pragma warning disable CA1031 // Last-resort barrier: nothing may escape an async void handler
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Failed to import data");
            _ = MessageBox.Show("Import data failed.\nMore details in the activities.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
         }
         finally
         {
            this.SetIsBusy(false);
         }
      }

      private async void _export_json_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IDatabase? database = _database;

         if (this.GetIsBusy()
            || database?.User is null)
         {
            return;
         }

         if (database.User.HasChanged()
            && MessageBox.Show("Before exporting data, all unsaved changes will be saved.", "Export data", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = "Export settings and services to a JSON file",
            Filter = "json file|*.json",
            FileName = $"{database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}",
         };

         if (!(dialog.ShowDialog() ?? false)) return;

         _export(database, dialog.FileName);
      }

      private async void _export_csv_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IDatabase? database = _database;

         if (this.GetIsBusy()
            || database?.User is null)
         {
            return;
         }

         if (database.User.HasChanged()
            && MessageBox.Show("Before exporting data, all unsaved changes will be saved.", "Export data", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = "Export services to a CSV file",
            Filter = "Tab delimited CSV file|*.csv",
            FileName = $"{database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}",
         };

         if (!(dialog.ShowDialog() ?? false)) return;

         _export(database, dialog.FileName);
      }

      private async void _export(IDatabase database, string fileName)
      {
         this.SetIsBusy(true);

         try
         {
            bool exported = await database.ExportToFileAsync(fileName).ConfigureAwait(true);

            _ = exported
               ? MessageBox.Show("Export data has been completed successfully.\nMore details in the activities.", "Export success")
               : MessageBox.Show("Export data failed.\nMore details in the activities.", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
         }
#pragma warning disable CA1031 // Last-resort barrier: nothing may escape an async void handler
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Failed to export data");
            _ = MessageBox.Show("Export data failed.\nMore details in the activities.", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
         }
         finally
         {
            this.SetIsBusy(false);
         }
      }
   }
}
