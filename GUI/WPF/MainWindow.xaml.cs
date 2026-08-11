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
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by WPF via XAML/BAML.")]
   internal sealed partial class MainWindow : Window
   {
      private readonly MainViewModel _mainViewModel;
      private readonly DispatcherTimer _timer;

      private static ISessionService _session => AppServices.Session;

      private bool _isClosing;

      // Opening a database and stretching a passkey are awaited, so the window
      // keeps pumping messages while they run. This guard is what stops a second
      // Enter from starting a concurrent attempt on the same progressive login
      // stack.
      private bool _isBusy;

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
         Closed += (s, e) => _isClosing = true;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         // While Open/Login is running the timer is stopped; this guard is a
         // safety net if a tick was already queued.
         if (_isBusy) return;

         _resetCredentials();
         _session.EndSession();
      }

      private async void _credential_TB_KeyUp(object sender, KeyEventArgs e)
      {
         // Every branch below can run while an open or a login is in flight, so
         // the guard comes first: a second Enter would race the progressive stack,
         // and menus are already disabled via MenusEnabled.
         if (_isBusy) return;

         if (e.Key == Key.Enter)
         {
            // Stop for the duration of Open/Login; restarted below if the attempt
            // leaves the user still on the credential screen.
            _timer.Stop();

            try
            {
               if (sender == _username_TB)
               {
                  await _submitUsernameAsync().ConfigureAwait(true);
               }
               else
               {
                  await _submitPasswordAsync().ConfigureAwait(true);
               }
            }
#pragma warning disable CA1031 // Last-resort barrier: nothing may escape an async void handler
            catch (Exception ex)
#pragma warning restore CA1031
            {
               Log.Error(ex, "Unexpected error while submitting credentials");
            }

            if (_isClosing) return;

            _password_PB.Clear();
            _timer.Start();
         }
         else if (e.Key == Key.Escape)
         {
            _resetCredentials();
            _session.EndSession();
         }
         else
         {
            // Idle timeout stays armed while the user types (username or passkeys).
            _timer.Stop();
            _timer.Start();
         }
      }

      private async Task _submitUsernameAsync()
      {
         if (string.IsNullOrEmpty(_username_TB.Text))
         {
            return;
         }

         if (!File.Exists(_mainViewModel.DatabaseFile))
         {
            string filename = AppServices.Cryptography.GetHash(_username_TB.Text);
            _mainViewModel.DatabaseFile = Path.GetFullPath($"{Path.GetDirectoryName(Environment.ProcessPath)}/raw/{filename}.pku");
         }

         try
         {
            IDatabase database = await Database.OpenAsync(AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               _mainViewModel.DatabaseFile,
               _username_TB.Text).ConfigureAwait(true);
            database.DatabaseClosed += _database_DatabaseClosed;
            database.AutoSaveDetected += _database_AutoSaveDetected;
            _session.StartSession(database);
         }
#pragma warning disable CA1031 // Last-resort barrier: a failed open is logged and surfaced, not propagated
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Failed to open database");
            if (ex is InsufficientKdfParametersException)
            {
               AppServices.Dialogs.Warn(
                  "This database file uses key-stretching parameters below the accepted security floor and cannot be opened.",
                  "Insufficient KDF parameters");
            }
            return;
         }

         // Open succeeded: lock menus for the rest of the progressive login and
         // switch to the passkey prompt. The idle timer is restarted by the
         // caller once this method returns.
         _mainViewModel.IsAwaitingPasskeys = true;
         _mainViewModel.CredentialsLabel = "Password :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Collapsed;

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Visible;
         _ = _password_PB.Focus();
      }

      private async Task _submitPasswordAsync()
      {
         IDatabase? database = _session.Database;

         if (_password_PB.SecurePassword.Length == 0
            || database is null)
         {
            return;
         }

         _setBusy("Checking the passkey...");

         try
         {
            // UseAsString zeroes the unmanaged buffer as soon as it returns, so
            // the task has to be started from inside it; only the managed copy it
            // captured outlives the call, for as long as the login needs it.
            Task<IUser?> login = _password_PB.SecurePassword.UseAsString(passkey => database.LoginAsync(passkey));

            // Erase the PasswordBox buffer right after submitting so the secret
            // is not kept alive longer than necessary.
            _password_PB.Clear();

            _ = await login.ConfigureAwait(true);
         }
         catch (CorruptedSourceException ex)
         {
            // Wrong passkeys stay soft (Login returns null). Corruption and other
            // hard failures bubble up so the user can be told and restart cleanly.
            Log.Error(ex, "Database corrupted during login");
            AppServices.Dialogs.Warn(
               "This database file appears to be corrupted or is not a valid Passkey vault and cannot be opened.",
               "Corrupted database");
            _resetCredentials();
            _session.EndSession();
            return;
         }
#pragma warning disable CA1031 // Last-resort barrier: an unexpected login error is shown to the user, not propagated
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Unexpected error during login");
            AppServices.Dialogs.Warn("An unexpected error occurred while opening the database.", "Login error");
            _resetCredentials();
            _session.EndSession();
            return;
         }
         finally
         {
            _password_PB.Clear();
            _clearBusy();
         }

         if (_session.Database?.User is null)
         {
            // Incomplete or failed layer: stay on the password prompt so the next
            // passkey can be entered (or Escape to abandon). The idle timer is
            // restarted by the caller once this method returns.
            _ = _password_PB.Focus();
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

      private void _setBusy(string message)
      {
         // Open/Login must not race with the idle reset: pause the timer for the
         // whole await, then let the caller restart it if the attempt failed.
         _timer.Stop();
         _isBusy = true;
         _mainViewModel.BusyMessage = message;
         _mainViewModel.IsBusy = true;
         this.SetIsBusy(true);
      }

      private void _clearBusy()
      {
         _isBusy = false;
         _mainViewModel.IsBusy = false;
         this.SetIsBusy(false);
      }

      private void _database_AutoSaveDetected(object? sender, Interfaces.Events.AutoSaveDetectedEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(e);

         // LoginAsync raises this from a worker thread and blocks on the answer,
         // so the prompt has to be marshalled back to the UI thread. That thread
         // is awaiting the login rather than blocking on it, which is what makes
         // this synchronous Invoke safe.
         e.MergeBehavior = Dispatcher.Invoke(() =>
         {
            Hide();

            MessageBoxResult result = AppServices.Dialogs.Confirm(
               "Unsaved changes have been detected.\nClick Yes to apply these changes.\nClick No to discard them.\nClick Cancel to ignore and keep the save file.",
               "Autosave detected",
               MessageBoxButton.YesNoCancel,
               MessageBoxImage.Question);

            return result switch
            {
               MessageBoxResult.Cancel => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile,
               MessageBoxResult.No => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile,
               _ => Passkey.Interfaces.Enums.AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile,
            };
         });
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
      {
         if (_isClosing || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
         {
            return;
         }

         _ = Dispatcher.BeginInvoke(() =>
         {
            if (_isClosing || !IsLoaded)
            {
               return;
            }

            _resetCredentials();
            _session.EndSession();
            Show();
         });
      }

      private void _resetCredentials()
      {
         _timer.Stop();

         _mainViewModel.IsAwaitingPasskeys = false;
         _mainViewModel.DatabaseFile = string.Empty;
         _mainViewModel.CredentialsLabel = "Username :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Collapsed;
      }
   }
}
