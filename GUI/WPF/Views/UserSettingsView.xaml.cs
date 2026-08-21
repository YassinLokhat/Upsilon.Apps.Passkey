using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
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
            ? Strings.Msg_UsernameEmpty
            : !_passwordsContainer.Passkeys.Any()
            ? Strings.Msg_AtLeastOnePassword
            : _passwordsContainer.Passkeys.Any(string.IsNullOrEmpty)
            ? Strings.Msg_NoPasswordEmpty
            : string.Empty;
      }

      private void _deleteUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _database?.User is null
            || MessageBox.Show(Strings.Msg_DeleteUserConfirm1, Strings.Title_ConfirmationRequired, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes
            || MessageBox.Show(Strings.Msg_DeleteUserConfirm2, Strings.Title_ConfirmationRequired, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         _ = Path.GetDirectoryName(_database.DatabaseFile) ?? string.Empty;

         _database.Delete();

         _ = MessageBox.Show(Strings.Format(nameof(Strings.Msg_UserDeleted), _viewModel.Username), Strings.Title_Success);
      }

      private async Task _saveAsync()
      {
         string error = _canSave();
         if (!string.IsNullOrEmpty(error))
         {
            _ = MessageBox.Show(error, Strings.Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);

            return;
         }

         string newFilename = AppServices.Cryptography.GetHash(_viewModel.Username);
         string newDatabaseFile = Path.GetFullPath($"{Path.Join(AppInfo.AppSettings.DefaultDatabaseDirectory, newFilename + ".pku")}");

         bool newUser = false;
         bool credentialsChanged = false;
         string oldDatabaseFile = string.Empty;

         if (_database?.User is null)
         {
            if (MessageBox.Show(Strings.Format(nameof(Strings.Msg_UseDefaultLocation), newDatabaseFile), Strings.Title_UseDefaultLocation, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
               SaveFileDialog dialog = new()
               {
                  Title = Strings.Title_NewUserDatabase,
                  Filter = Strings.Filter_Pku,
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

         string message;

         if (credentialsChanged)
         {
            message = Strings.Format(nameof(Strings.Msg_CredentialsUpdated), _viewModel.Username);
            _passwordsContainer.ClearSecrets();
            _session.EndSession();

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
            message = Strings.Format(nameof(Strings.Msg_UserCreated), _viewModel.Username);
            _passwordsContainer.ClearSecrets();
            _session.EndSession();
         }
         else
         {
            message = Strings.Format(nameof(Strings.Msg_UserUpdated), _viewModel.Username);
            this.DatabaseClosed(_isClosing);
         }

         _ = MessageBox.Show(message, Strings.Title_Success);
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

         if (MessageBox.Show(Strings.Msg_SaveBeforeContinue, title, MessageBoxButton.OKCancel)
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

         if (!await _savePendingChangesAsync(database, Strings.Msg_ImportData).ConfigureAwait(true))
         {
            return;
         }

         OpenFileDialog dialog = new()
         {
            Title = Strings.Title_ImportData,
            Filter = $"{Strings.Filter_Json}|{Strings.Filter_Csv}",
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
               ? MessageBox.Show(Strings.Msg_ImportSuccess, Strings.Title_ImportSuccess)
               : MessageBox.Show(Strings.Msg_ImportFailed, Strings.Title_ImportFailed, MessageBoxButton.OK, MessageBoxImage.Error);
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

         if (!await _savePendingChangesAsync(database, Strings.Msg_ExportData).ConfigureAwait(true))
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = Strings.Title_ExportJson,
            Filter = Strings.Filter_Json,
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

         if (!await _savePendingChangesAsync(database, Strings.Msg_ExportData).ConfigureAwait(true))
         {
            return;
         }

         SaveFileDialog dialog = new()
         {
            Title = Strings.Title_ExportCsv,
            Filter = Strings.Filter_Csv,
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
               ? MessageBox.Show(Strings.Msg_ExportSuccess, Strings.Title_ExportSuccess)
               : MessageBox.Show(Strings.Msg_ExportFailed, Strings.Title_ExportFailed, MessageBoxButton.OK, MessageBoxImage.Error);
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
   }
}
