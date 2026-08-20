using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Core.Utils.LeakFilter;
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
            = _viewActivities_MI.Visibility
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

         _passwordsContainer.ClearSecrets();
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
            if (_viewModel.NotifyActivityReview)
            {
               warningsToNotify |= WarningType.ActivityReviewWarning;
            }

            if (_viewModel.NotifyDuplicatedPasswords)
            {
               warningsToNotify |= WarningType.DuplicatedPasswordsWarning;
            }

            if (_viewModel.NotifyPasswordUpdateReminder)
            {
               warningsToNotify |= WarningType.PasswordUpdateReminderWarning;
            }

            if (_viewModel.NotifyPasswordLeaked)
            {
               warningsToNotify |= WarningType.PasswordLeakedWarning;
            }

            _database.User.Settings.WarningsToNotify = warningsToNotify;

            await _database.SaveAsync().ConfigureAwait(true);
         }

         string message = $"'{_viewModel.Username}' user database ";

         if (credentialsChanged)
         {
            message = $"'{_viewModel.Username}' user's credentials has been updated.\nYou will be logged out.\nPlease login again.";
            _passwordsContainer.ClearSecrets();
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
            _passwordsContainer.ClearSecrets();
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
         if (this.GetIsBusy())
         {
            return;
         }

         this.SetIsBusy(true);

         try
         {
            await _saveAsync().ConfigureAwait(true);
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

      private static async Task<bool> _savePendingChangesAsync(IDatabase database, string title)
      {
         if (!database.User!.HasChanged())
         {
            return true;
         }

         if (MessageBox.Show("Before continuing, all unsaved changes will be saved.", title, MessageBoxButton.OKCancel)
            != MessageBoxResult.OK)
         {
            return false;
         }

         await database.SaveAsync().ConfigureAwait(true);
         return true;
      }

      private async void _import_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IDatabase? database = _database;

         if (this.GetIsBusy()
            || database?.User is null)
         {
            return;
         }

         if (!await _savePendingChangesAsync(database, "Import data").ConfigureAwait(true))
         {
            return;
         }

         OpenFileDialog dialog = new()
         {
            Title = "Import data from a file",
            Filter = "json file|*.json|Tab delimited CSV file|*.csv",
         };

         if (!(dialog.ShowDialog() ?? false))
         {
            return;
         }

         this.SetIsBusy(true);

         try
         {
            bool imported = await database.ImportFromFileAsync(dialog.FileName).ConfigureAwait(true);

            _ = imported
               ? MessageBox.Show("Import data has been completed successfully.\nMore details in the activities.", "Import success")
               : MessageBox.Show("Import data failed.\nMore details in the activities.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

         if (!await _savePendingChangesAsync(database, "Export data").ConfigureAwait(true))
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = "Export settings and services to a JSON file",
            Filter = "json file|*.json",
            FileName = $"{database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}",
         };

         if (!(dialog.ShowDialog() ?? false))
         {
            return;
         }

         await _exportAsync(database, dialog.FileName).ConfigureAwait(true);
      }

      private async void _export_csv_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IDatabase? database = _database;

         if (this.GetIsBusy()
            || database?.User is null)
         {
            return;
         }

         if (!await _savePendingChangesAsync(database, "Export data").ConfigureAwait(true))
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = "Export services to a CSV file",
            Filter = "Tab delimited CSV file|*.csv",
            FileName = $"{database.User.ItemId ?? string.Empty}-{DateTime.Now:yyyyMMddHHmm}",
         };

         if (!(dialog.ShowDialog() ?? false))
         {
            return;
         }

         await _exportAsync(database, dialog.FileName).ConfigureAwait(true);
      }

      private async Task _exportAsync(IDatabase database, string fileName)
      {
         this.SetIsBusy(true);

         try
         {
            bool exported = await database.ExportToFileAsync(fileName).ConfigureAwait(true);

            _ = exported
               ? MessageBox.Show("Export data has been completed successfully.\nMore details in the activities.", "Export success")
               : MessageBox.Show("Export data failed.\nMore details in the activities.", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
         }
         finally
         {
            this.SetIsBusy(false);
         }
      }

      private void _viewActivities_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null
            || AppServices.Session.User is null)
         {
            return;
         }

         string itemId = AppServices.Session.User.ItemId;

         _ = AppServices.Dialogs.ShowSingleton(
            factory: () =>
            {
               UserActivitiesView view = new(needsReviewFilter: false);
               view.ViewModel.ClearFilters();
               view.ViewModel.SearchCriteria = itemId;
               return view;
            },
            configure: view => view.ViewModel.SearchCriteria = itemId);
      }

      private void _offlineLeakFilterEnabled_Changed(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         LeakFilterConfig config = LeakFilterPaths.LoadConfig();
         config.Enabled = _viewModel.OfflineLeakFilterEnabled;
         LeakFilterPaths.SaveConfig(config);

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter();
         }

         _viewModel.RefreshOfflineLeakFilterStatus();
      }

      private async void _offlineLeakFilterBuild_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         bool force = File.Exists(LeakFilterPaths.ResolveFilterFilePath());
         if (force
            && AppServices.Dialogs.Confirm(
               "An offline leak database already exists. Rebuild it from HIBP?\nThis can take several hours and uses a large download.",
               "Rebuild offline leak database") != MessageBoxResult.Yes)
         {
            return;
         }

         if (!force
            && AppServices.Dialogs.Confirm(
               "Download the HIBP password corpus and build a local Bloom filter (~2.4 GiB)?\nThis is shared by all vault users on this machine and can take several hours.",
               "Build offline leak database") != MessageBoxResult.Yes)
         {
            return;
         }

         _viewModel.OfflineLeakFilterBusy = true;
         _viewModel.OfflineLeakFilterProgress = "Starting…";

         try
         {
            Progress<HibpBloomBuildProgress> progress = new(p =>
            {
               if (p.Skipped)
               {
                  _viewModel.OfflineLeakFilterProgress = "Skipped (file already present).";
                  return;
               }

               double pct = 100.0 * p.CompletedPrefixes / p.TotalPrefixes;
               _viewModel.OfflineLeakFilterProgress =
                  $"{pct:0.00}% · prefixes {p.CompletedPrefixes}/{p.TotalPrefixes} · hashes ≈ {p.InsertedHashes}";
            });

            string filterPath = LeakFilterPaths.ResolveFilterFilePath();
            HibpBloomBuildResult result = await HibpBloomBuilder.BuildAsync(
               filterPath,
               force: force,
               progress: progress).ConfigureAwait(true);

            LeakFilterConfig config = LeakFilterPaths.LoadConfig();
            config.Enabled = true;
            LeakFilterPaths.SaveConfig(config);
            _viewModel.OfflineLeakFilterEnabled = true;

            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.ReloadLocalFilter();
            }

            _viewModel.OfflineLeakFilterProgress = result.Skipped
               ? "Already up to date."
               : $"Build complete ({result.InsertedCount} hashes).";
            _viewModel.RefreshOfflineLeakFilterStatus();
         }
#pragma warning disable CA1031 // UI boundary: surface build failures as a dialog
         catch (Exception ex)
#pragma warning restore CA1031
         {
            AppServices.Dialogs.Warn($"Offline leak database build failed:\n{ex.Message}", "Build failed");
            _viewModel.OfflineLeakFilterProgress = "Build failed.";
         }
         finally
         {
            _viewModel.OfflineLeakFilterBusy = false;
         }
      }

      private void _offlineLeakFilterDelete_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         if (!File.Exists(LeakFilterPaths.ResolveFilterFilePath()))
         {
            AppServices.Dialogs.Info("No offline leak database file is present.", "Offline leak database");
            return;
         }

         if (AppServices.Dialogs.Confirm(
               "Permanently delete the shared offline leak database from this machine?\nThis affects all vault users. Disabling the option alone does not delete the file.",
               "Delete offline leak database",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.AttachLocalFilter(null);
         }

         _ = LeakFilterPaths.TryDeleteFilterFile();
         _viewModel.OfflineLeakFilterProgress = string.Empty;
         _viewModel.RefreshOfflineLeakFilterStatus();
      }
   }
}
