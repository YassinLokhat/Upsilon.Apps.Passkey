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
   /// Interaction logic for UserView.xaml
   /// </summary>
   public partial class UserView : Window
   {
      private readonly UserViewModel _viewModel;
      private readonly PasswordsContainer _passwordsContainer;

      public UserView()
      {
         InitializeComponent();

         _passwordsContainer = new(MainViewModel.Database?.User?.Passkeys);
         _credentials.Children.Add(_passwordsContainer);
         _deleteUser.Visibility = (MainViewModel.Database is null || MainViewModel.Database.User is null) ? Visibility.Hidden : Visibility.Visible;

         DataContext = _viewModel = new UserViewModel();

         Loaded += _mainWindow_Loaded;
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
         if (string.IsNullOrEmpty(_viewModel.Username))
         {
            return "Username cannot be empty.";
         }

         if (_passwordsContainer.Passkeys.Length == 0)
         {
            return "At least one password should be set.";
         }

         if (_passwordsContainer.Passkeys.Any(x => string.IsNullOrEmpty(x)))
         {
            return "No password can be empty.";
         }

         return string.Empty;
      }

      private void _deleteUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         /// TODO : To implement
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         string error = _canSave();
         if (!string.IsNullOrEmpty(error))
         {
            MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
         }

         string newFilename = MainViewModel.CryptographyCenter.GetHash(_viewModel.Username);
         string databaseFile = Path.GetFullPath($"{newFilename}/{newFilename}.pku");
         string autoSaveFile = Path.GetFullPath($"{newFilename}/{newFilename}.pks");
         string logFile = Path.GetFullPath($"{newFilename}/{newFilename}.pkl");
         
         if (MainViewModel.Database is null
            || MainViewModel.Database.User is null)
         {
            try
            {
               MainViewModel.Database = IDatabase.Create(MainViewModel.CryptographyCenter,
                  MainViewModel.SerializationCenter,
                  MainViewModel.PasswordFactory,
                  databaseFile,
                  autoSaveFile,
                  logFile,
                  _viewModel.Username,
                  _passwordsContainer.Passkeys);

               MainViewModel.Database.Close();
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               return;
            }
         }
         else
         {
            string oldFileName = MainViewModel.CryptographyCenter.GetHash(MainViewModel.Database.User.Username);

            bool credentialsChanged = _credentialsChanged(oldFileName,
               oldPasskeys: MainViewModel.Database.User.Passkeys,
               newFilename,
               newPasskeys: _passwordsContainer.Passkeys);

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

            if (credentialsChanged)
            {
               MessageBox.Show("User credentials has been updated.\nYou will be logged out.\nPlease login again.");

               MainViewModel.Database.Close();

               if (File.Exists(databaseFile))
               {
                  File.Delete(databaseFile);
               }

               if (File.Exists(autoSaveFile))
               {
                  File.Delete(autoSaveFile);
               }

               if (File.Exists(logFile))
               {
                  File.Delete(logFile);
               }
            }
         }

         DialogResult = true;
      }

      private static bool _credentialsChanged(string oldFileName, string[] oldPasskeys, string newFilename, string[] newPasskeys)
      {
         return oldFileName != newFilename || ISerializationCenter.AreDifferent(MainViewModel.SerializationCenter, oldPasskeys, newPasskeys);
      }
   }
}
