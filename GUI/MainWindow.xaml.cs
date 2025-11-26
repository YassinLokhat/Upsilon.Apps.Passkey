using System.IO;
using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
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

         _resetCredentials(resetDatabase: true);

         _username_TB.KeyUp += _credential_TB_KeyUp;
         _password_PB.KeyUp += _credential_TB_KeyUp;
         _timer.Tick += _timer_Elapsed;
         Loaded += _mainWindow_Loaded;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         _resetCredentials(resetDatabase: true);
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);

         /// TODO : To be removed
         try
         {
            string filename = MainViewModel.CryptographyCenter.GetHash("_");
            string databaseFile = Path.GetFullPath($"raw/{filename}/{filename}.pku");
            string autoSaveFile = Path.GetFullPath($"raw/{filename}/{filename}.pks");
            string logFile = Path.GetFullPath($"raw/{filename}/{filename}.pkl");

            Hide();

            MainViewModel.Database = IDatabase.Open(MainViewModel.CryptographyCenter,
               MainViewModel.SerializationCenter,
               MainViewModel.PasswordFactory,
               databaseFile,
               autoSaveFile,
               logFile,
               "_");
            MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
            MainViewModel.Database.AutoSaveDetected += (s, e) => e.MergeBehavior = Passkey.Core.Public.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile;
            MainViewModel.Database.WarningDetected += _database_WarningDetected;
            _ = MainViewModel.Database.Login("a");
            _ = MainViewModel.Database.Login("b");
            _ = MainViewModel.Database.Login("c");
            _resetCredentials(resetDatabase: false);
            if (!UserServicesView.ShowUser(this))
            {
               Close();
            }
         }
         catch
         {
            MainViewModel.Database?.Close();
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

      private void _credential_TB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            if (sender == _username_TB)
            {
               if (string.IsNullOrEmpty(_username_TB.Text))
               {
                  return;
               }

               string filename = MainViewModel.CryptographyCenter.GetHash(_username_TB.Text);
               string databaseFile = Path.GetFullPath($"raw/{filename}/{filename}.pku");
               string autoSaveFile = Path.GetFullPath($"raw/{filename}/{filename}.pks");
               string logFile = Path.GetFullPath($"raw/{filename}/{filename}.pkl");

               try
               {
                  MainViewModel.Database = IDatabase.Open(MainViewModel.CryptographyCenter,
                     MainViewModel.SerializationCenter,
                     MainViewModel.PasswordFactory,
                     databaseFile,
                     autoSaveFile,
                     logFile,
                     _username_TB.Text);

                  MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
                  MainViewModel.Database.AutoSaveDetected += _database_AutoSaveDetected;
                  MainViewModel.Database.WarningDetected += _database_WarningDetected;
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
                  return;
               }

               if (MainViewModel.Database is not null)
               {
                  _ = MainViewModel.Database.Login(_password_PB.Password);

                  if (MainViewModel.Database.User is not null)
                  {
                     Hide();

                     _resetCredentials(resetDatabase: false);

                     if (!UserServicesView.ShowUser(this))
                     {
                        Close();
                     }
                  }
               }
            }

            _password_PB.Password = string.Empty;
            _timer.Stop();
            _timer.Start();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            _resetCredentials(resetDatabase: true);
         }
      }

      private void _database_WarningDetected(object? sender, Passkey.Core.Public.Events.WarningDetectedEventArgs e)
      {
         /// TODO : Pending implement
      }

      private void _database_AutoSaveDetected(object? sender, Passkey.Core.Public.Events.AutoSaveDetectedEventArgs e)
      {
         MessageBoxResult result = MessageBox.Show("Unsaved changes have been detected.\nClick Yes to apply these changes.\nClick No to discard them.\nClick Cancel to ignore and keep the save file.", "Autosave detected", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

         e.MergeBehavior = result switch
         {
            MessageBoxResult.Cancel => Passkey.Core.Public.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
            MessageBoxResult.No => Passkey.Core.Public.Enums.AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
            _ => Passkey.Core.Public.Enums.AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
         };
      }

      private void _database_DatabaseClosed(object? sender, Passkey.Core.Public.Events.LogoutEventArgs e)
      {
         try
         {
            Dispatcher.Invoke(() =>
            {
               _resetCredentials(resetDatabase: true);
               Show();
            });
         }
         catch { }
      }

      private void _resetCredentials(bool resetDatabase)
      {
         _mainViewModel.Label = "Username :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Password = string.Empty;
         _password_PB.Visibility = Visibility.Hidden;

         if (resetDatabase)
         {
            MainViewModel.Database = null;
         }

         _timer.Stop();
      }
   }
}