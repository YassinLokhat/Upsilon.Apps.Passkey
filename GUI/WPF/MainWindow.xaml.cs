using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private readonly MainViewModel _mainViewModel;
      private readonly DispatcherTimer _timer;

      private static ISessionService Session => AppServices.Session;

      public MainWindow()
      {
         InitializeComponent();

         DataContext = _mainViewModel = new MainViewModel();
         _mainViewModel.ResetRequested += (_, _) => _resetCredentials();

         _timer = new()
         {
            Interval = TimeSpan.FromSeconds(5),
         };

         _resetCredentials();

         string[] args = Environment.GetCommandLineArgs();
         if (args.Length > 1)
         {
            try
            {
               string databaseFile = Path.GetFullPath(args[1]);
               if (File.Exists(databaseFile))
               {
                  _mainViewModel.DatabaseFile = databaseFile;
               }
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
               Log.Warn($"Ignored invalid database path from command line: {ex.Message}");
            }
         }

         _username_TB.KeyUp += _credential_TB_KeyUp;
         _password_PB.KeyUp += _credential_TB_KeyUp;
         _timer.Tick += _timer_Elapsed;
         Loaded += (s, e) => this.PostLoadSetup();
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         _resetCredentials();
         Session.EndSession();
      }

      private void _credential_TB_KeyUp(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Enter)
         {
            _timer.Stop();

            if (sender == _username_TB)
            {
               _submitUsername();
            }
            else
            {
               _submitPassword();
            }

            _password_PB.Clear();
            _timer.Start();
         }
         else if (e.Key == Key.Escape)
         {
            _resetCredentials();
            Session.EndSession();
         }
         else
         {
            _timer.Stop();
            _timer.Start();
         }
      }

      private void _submitUsername()
      {
         if (string.IsNullOrEmpty(_username_TB.Text))
         {
            _timer.Start();
            return;
         }

         if (!File.Exists(_mainViewModel.DatabaseFile))
         {
            string filename = AppServices.Cryptography.GetHash(_username_TB.Text);
            _mainViewModel.DatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{filename}.pku");
         }

         try
         {
            IDatabase database = Database.Open(AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               _mainViewModel.DatabaseFile,
               _username_TB.Text);

            database.DatabaseClosed += _database_DatabaseClosed;
            database.AutoSaveDetected += _database_AutoSaveDetected;
            Session.StartSession(database);
         }
         catch (Exception ex)
         {
            Log.Error(ex, "Failed to open database");
         }

         _mainViewModel.CredentialsLabel = "Password :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Collapsed;

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Visible;
         _ = _password_PB.Focus();
      }

      private void _submitPassword()
      {
         if (_password_PB.SecurePassword.Length == 0)
         {
            _timer.Start();
            return;
         }

         if (Session.Database is null)
         {
            return;
         }

         try
         {
            _ = Session.Database.Login(_password_PB.SecurePassword);
         }
         catch (Exception ex)
         {
            Log.Error(ex, "Unexpected error during login");
            AppServices.Dialogs.Warn("An unexpected error occurred while opening the database.", "Login error");
            return;
         }
         finally
         {
            // Erase the PasswordBox buffer right after submitting so the secret
            // is not kept alive longer than necessary.
            _password_PB.Clear();
         }

         if (Session.Database.User is null)
         {
            return;
         }

         Hide();
         _resetCredentials();

         if (!UserServicesView.ShowUser(this))
         {
            Close();
         }
         else
         {
            _resetCredentials();
         }
      }

      private void _database_AutoSaveDetected(object? sender, Interfaces.Events.AutoSaveDetectedEventArgs e)
      {
         Hide();

         MessageBoxResult result = AppServices.Dialogs.Confirm(
            "Unsaved changes have been detected.\nClick Yes to apply these changes.\nClick No to discard them.\nClick Cancel to ignore and keep the save file.",
            "Autosave detected",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

         e.MergeBehavior = result switch
         {
            MessageBoxResult.Cancel => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
            MessageBoxResult.No => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
            _ => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
         };
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
      {
         if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
         {
            return;
         }

         _ = Dispatcher.BeginInvoke(() =>
         {
            _resetCredentials();
            Session.EndSession();
            Show();
         });
      }

      private void _resetCredentials()
      {
         _mainViewModel.DatabaseFile = string.Empty;
         _mainViewModel.CredentialsLabel = "Username :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Collapsed;

         _timer.Stop();
      }
   }
}
