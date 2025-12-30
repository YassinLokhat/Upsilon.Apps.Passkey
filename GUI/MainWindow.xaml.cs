using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.Views;

namespace Upsilon.Apps.Passkey.GUI
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private readonly MainViewModel _mainViewModel;
      private readonly DispatcherTimer _timer;

      public MainWindow()
      {
         InitializeComponent();

         DataContext = _mainViewModel = new MainViewModel();

         _timer = new()
         {
            Interval = new TimeSpan(0, 0, 5),
         };

         _resetCredentials();
         MainViewModel.Database = null;

         _username_TB.KeyUp += _credential_TB_KeyUp;
         _password_PB.KeyUp += _credential_TB_KeyUp;
         _timer.Tick += _timer_Elapsed;
         Loaded += _mainWindow_Loaded;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         _resetCredentials();
         MainViewModel.Database = null;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);

         /// TODO : To be removed
         try
         {
            string filename = MainViewModel.CryptographyCenter.GetHash("_");
            string databaseFile = Path.GetFullPath($"raw/{filename}.pku");

            MainViewModel.Database = Database.Open(MainViewModel.CryptographyCenter,
               MainViewModel.SerializationCenter,
               MainViewModel.PasswordFactory,
               MainViewModel.ClipboardManager,
               databaseFile,
               "_");
            MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
            MainViewModel.Database.AutoSaveDetected += (s, e) => e.MergeBehavior = Interfaces.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile;
            _ = MainViewModel.Database.Login("a");
            _ = MainViewModel.Database.Login("b");
            _ = MainViewModel.Database.Login("c");
            _resetCredentials();

            if (MainViewModel.Database?.User is not null)
            {
               Hide();

               if (!UserServicesView.ShowUser(this))
               {
                  Close();
               }
            }
            else
            {
               MainViewModel.Database?.Close();
               MainViewModel.Database = null;
            }
         }
         catch
         {
            MainViewModel.Database?.Close();
            MainViewModel.Database = null;
         }
      }

      private void _newUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         UserSettingsView.ShowUserSettings(this);
      }

      private void _generatePassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         _ = PasswordGenerator.ShowGeneratePasswordDialog(this);
      }

      private void _credential_TB_KeyUp(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter)
         {
            _timer.Stop();

            if (sender == _username_TB)
            {
               if (string.IsNullOrEmpty(_username_TB.Text))
               {
                  _timer.Start();
                  return;
               }

               string filename = MainViewModel.CryptographyCenter.GetHash(_username_TB.Text);
               string databaseFile = Path.GetFullPath($"raw/{filename}.pku");

               try
               {
                  MainViewModel.Database = Database.Open(MainViewModel.CryptographyCenter,
                     MainViewModel.SerializationCenter,
                     MainViewModel.PasswordFactory,
                     MainViewModel.ClipboardManager,
                     databaseFile,
                     _username_TB.Text);

                  MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                  MainViewModel.Database.AutoSaveDetected += _database_AutoSaveDetected;
               }
               catch { }

               _mainViewModel.Label = "Password :";

               _username_TB.Text = string.Empty;
               _username_TB.Visibility = Visibility.Hidden;

               _password_PB.Password = string.Empty;
               _password_PB.Visibility = Visibility.Visible;
               _ = _password_PB.Focus();
            }
            else
            {
               if (string.IsNullOrEmpty(_password_PB.Password))
               {
                  _timer.Start();
                  return;
               }

               if (MainViewModel.Database is not null)
               {
                  _ = MainViewModel.Database.Login(_password_PB.Password);

                  if (MainViewModel.Database.User is not null)
                  {
                     Hide();
                     _resetCredentials();

                     if (!UserServicesView.ShowUser(this))
                     {
                        Close();
                     }
                  }
               }
            }

            _password_PB.Password = string.Empty;
            _timer.Start();
         }
         else if (e.Key == Key.Escape)
         {
            _resetCredentials();
            MainViewModel.Database = null;
         }
      }

      private void _database_AutoSaveDetected(object? sender, Interfaces.Events.AutoSaveDetectedEventArgs e)
      {
         MessageBoxResult result = MessageBox.Show("Unsaved changes have been detected.\nClick Yes to apply these changes.\nClick No to discard them.\nClick Cancel to ignore and keep the save file.", "Autosave detected", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

         e.MergeBehavior = result switch
         {
            MessageBoxResult.Cancel => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
            MessageBoxResult.No => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
            _ => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
         };
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
      {
         try
         {
            Dispatcher.Invoke(() =>
            {
               _resetCredentials();
               MainViewModel.Database = null;
               Show();
            });
         }
         catch { }
      }

      private void _resetCredentials()
      {
         _mainViewModel.Label = "Username :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Password = string.Empty;
         _password_PB.Visibility = Visibility.Hidden;

         _timer.Stop();
      }
   }
}