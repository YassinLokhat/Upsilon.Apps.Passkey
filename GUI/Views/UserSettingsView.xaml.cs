using System.IO;
using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.Views.Controls;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserSettingsView.xaml
   /// </summary>
   public partial class UserSettingsView : Window
   {
      private readonly UserSettingsViewModel _viewModel;
      private readonly PasswordsContainer _passwordsContainer;

      public UserSettingsView()
      {
         InitializeComponent();

         _passwordsContainer = new(MainViewModel.Database?.User?.Passkeys);
         _ = _credentials.Children.Add(_passwordsContainer);
         _deleteUser.Visibility = (MainViewModel.Database is null || MainViewModel.Database.User is null) ? Visibility.Hidden : Visibility.Visible;

         DataContext = _viewModel = new UserSettingsViewModel();

         if (MainViewModel.Database is not null
            && MainViewModel.Database.User is not null)
         {
            MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         }

         Loaded += _mainWindow_Loaded;
      }

      public static void ShowUserSettings(Window owner)
      {
         _ = new UserSettingsView()
         {
            Owner = owner
         }
         .ShowDialog();
      }

      private void _database_DatabaseClosed(object? sender, PassKey.Core.Public.Events.LogoutEventArgs e)
      {
         DialogResult = true;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      private void _value_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
         if (MainViewModel.Database is null
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

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         string error = _canSave();
         if (!string.IsNullOrEmpty(error))
         {
            _ = MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
         }

         string newFilename = MainViewModel.CryptographyCenter.GetHash(_viewModel.Username);
         string newDatabaseFile = Path.GetFullPath($"raw/{newFilename}/{newFilename}.pku");
         string newAutoSaveFile = Path.GetFullPath($"raw/{newFilename}/{newFilename}.pks");
         string newLogFile = Path.GetFullPath($"raw/{newFilename}/{newFilename}.pkl");

         bool newUser = false;
         bool credentialsChanged = false;
         string oldDatabaseFile = string.Empty;
         string oldAutoSaveFile = string.Empty;
         string oldLogFile = string.Empty;

         if (MainViewModel.Database is null
            || MainViewModel.Database.User is null)
         {
            try
            {
               MainViewModel.Database = IDatabase.Create(MainViewModel.CryptographyCenter,
                  MainViewModel.SerializationCenter,
                  MainViewModel.PasswordFactory,
                  newDatabaseFile,
                  newAutoSaveFile,
                  newLogFile,
                  _viewModel.Username,
                  _passwordsContainer.Passkeys);

               foreach (string passkey in _passwordsContainer.Passkeys)
               {
                  _ = MainViewModel.Database.Login(passkey);
               }
            }
            catch (Exception ex)
            {
               _ = MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               return;
            }

            newUser = true;
         }
         else
         {
            string oldFileName = MainViewModel.CryptographyCenter.GetHash(MainViewModel.Database.User.Username);
            oldDatabaseFile = Path.GetFullPath($"raw/{oldFileName}/{oldFileName}.pku");
            oldAutoSaveFile = Path.GetFullPath($"raw/{oldFileName}/{oldFileName}.pks");
            oldLogFile = Path.GetFullPath($"raw/{oldFileName}/{oldFileName}.pkl");

            credentialsChanged = _credentialsChanged(oldFileName,
               oldPasskeys: MainViewModel.Database.User.Passkeys,
               newFilename,
               newPasskeys: _passwordsContainer.Passkeys);
         }

         if (MainViewModel.Database.User != null)
         {
            MainViewModel.Database.User.Username = _viewModel.Username;
            MainViewModel.Database.User.Passkeys = _passwordsContainer.Passkeys;
            MainViewModel.Database.User.LogoutTimeout = _viewModel.LogoutTimeout;
            MainViewModel.Database.User.CleaningClipboardTimeout = _viewModel.CleaningClipboardTimeout;
            WarningType warningsToNotify = (WarningType)0;
            if (_viewModel.NotifyLogReview) warningsToNotify |= WarningType.LogReviewWarning;
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

            if (!Directory.Exists(newDatabaseDirectory))
            {
               Directory.CreateDirectory(newDatabaseDirectory);
            }

            if (File.Exists(oldDatabaseFile))
            {
               File.Move(oldDatabaseFile, newDatabaseFile);
            }

            if (File.Exists(oldAutoSaveFile))
            {
               File.Move(oldAutoSaveFile, newAutoSaveFile);
            }

            if (File.Exists(oldLogFile))
            {
               File.Move(oldLogFile, newLogFile);
            }

            if (Directory.Exists(oldDatabaseDirectory))
            {
               Directory.Delete(oldDatabaseDirectory, true);
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
            DialogResult = true;
         }

         _ = MessageBox.Show(message, "Success");
      }

      private static bool _credentialsChanged(string oldFileName, string[] oldPasskeys, string newFilename, string[] newPasskeys)
      {
         return oldFileName != newFilename || ISerializationCenter.AreDifferent(MainViewModel.SerializationCenter, oldPasskeys, newPasskeys);
      }
   }
}
