using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
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
      private readonly DispatcherTimer _idleTimer;

      private static ISessionService _session => AppServices.Session;

      private bool _isClosing;

      // Opening a database and stretching a passkey are awaited, so the window
      // keeps pumping messages while they run. This guard is what stops a second
      // Enter from starting a concurrent attempt on the same progressive login
      // stack, and what ignores Escape until the in-flight Open/Login finishes.
      private bool _isBusy;

      // Seconds remaining before the idle reset fires. Only meaningful while the
      // one-second idle timer is running (LoginIdleTimeoutSeconds > 0).
      private int _idleSecondsRemaining;

      public MainWindow()
      {
         InitializeComponent();

         DataContext = _mainViewModel = new MainViewModel();
         _mainViewModel.ResetRequested += (_, _) => _resetCredentials();

         _idleTimer = new()
         {
            Interval = TimeSpan.FromSeconds(1),
         };

         _resetCredentials();

         _handleCommandLineArgs();

         _username_TB.KeyUp += _credential_TB_KeyUp;
         _password_PB.KeyUp += _credential_TB_KeyUp;
         _idleTimer.Tick += _idleTimer_Tick;
         Loaded += _mainWindow_Loaded;
         Closed += _window_Closed;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         this.PostLoadSetup();

         if (AppInfo.TryConsumeConfigLoadError())
         {
            AppServices.Dialogs.Warn(Strings.Msg_ConfigFileError, Strings.Title_ConfigFileError);
         }
      }

      private void _window_Closed(object? sender, EventArgs e)
      {
         _isClosing = true;
         _stopIdleTimer();
         _endSession();
      }

      private void _handleCommandLineArgs()
      {
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

         if (args.Length > 2)
         {
            _username_TB.Text = args[2];
            _username_TB.SelectionStart = _username_TB.Text.Length;
            _username_TB.SelectionLength = 0;

            // Same as typing into the username box: keep the idle auto-reset
            // armed so a CLI-prefilled username does not linger forever.
            _armIdleTimer();
         }
      }

      private void _idleTimer_Tick(object? sender, EventArgs e)
      {
         // While Open/Login is running the timer is stopped; this guard is a
         // safety net if a tick was already queued.
         if (_isBusy)
         {
            return;
         }

         if (_idleSecondsRemaining > 1)
         {
            _idleSecondsRemaining--;
            _refreshIdleTitle();
            return;
         }

         _idleSecondsRemaining = 0;
         _stopIdleTimer();
         _resetCredentials();
         _endSession();
      }

      private async void _credential_TB_KeyUp(object sender, KeyEventArgs e)
      {
         // Every branch below can run while an open or a login is in flight, so
         // the guard comes first: a second Enter would race the progressive stack,
         // Escape would tear down a session that OpenAsync is about to publish,
         // and menus are already disabled via MenusEnabled.
         if (_isBusy)
         {
            return;
         }

         if (e.Key == Key.Enter)
         {
            // Stop for the duration of Open/Login; restarted below if the attempt
            // leaves the user still on the credential screen.
            _stopIdleTimer();

            if (sender.Equals(_username_TB))
            {
               await _submitUsernameAsync().ConfigureAwait(true);
            }
            else
            {
               await _submitPasswordAsync().ConfigureAwait(true);
            }

            if (_isClosing)
            {
               return;
            }

            _password_PB.Clear();

            // Only re-arm when there is still something to protect (half-open
            // session or typed username). A successful login already cleared the
            // form before returning here.
            if (_mainViewModel.IsAwaitingPasskeys
               || _session.Database is not null
               || !string.IsNullOrEmpty(_username_TB.Text))
            {
               _armIdleTimer();
            }
         }
         else if (e.Key == Key.Escape)
         {
            _resetCredentials();
            _endSession();
         }
         else
         {
            // Idle timeout stays armed while the user types (username or passkeys).
            _armIdleTimer();
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
            _mainViewModel.DatabaseFile = Path.GetFullPath($"{Path.Join(AppInfo.AppSettings.DefaultDatabaseDirectory, filename + ".pku")}");
         }

         _setBusy(Strings.Msg_OpeningDatabase);

         try
         {
            IDatabase database = await Database.OpenAsync(AppServices.Cryptography,
               AppServices.Serialization,
               AppServices.PasswordFactory,
               AppServices.Clipboard,
               _mainViewModel.DatabaseFile,
               _username_TB.Text).ConfigureAwait(true);

            if (_isClosing)
            {
               database.Close();
               return;
            }

            database.DatabaseClosed += _database_DatabaseClosed;
            database.AutoSaveDetected += _database_AutoSaveDetected;
            _session.StartSession(database);
         }
         catch (InsufficientKdfParametersException ex)
         {
            Log.Error(ex, "Failed to open database");
            AppServices.Dialogs.Warn(
                  Strings.Msg_InsufficientKdf,
                  Strings.Title_InsufficientKdf);
            return;
         }
         finally
         {
            _clearBusy();
         }

         if (_isClosing || _session.Database is null)
         {
            return;
         }

         // Open succeeded: lock menus for the rest of the progressive login and
         // switch to the passkey prompt. The idle timer is restarted by the
         // caller once this method returns.
         _mainViewModel.IsAwaitingPasskeys = true;
         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Collapsed;

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Visible;
         _ = _password_PB.Focus();
      }

      private async Task _submitPasswordAsync()
      {
         IDatabase? database = _session.Database;
         if (database is null)
         {
            return;
         }

         // PasswordBox.SecurePassword returns a new SecureString the caller must
         // dispose; keep a single copy for the length check and the login call.
         using SecureString securePassword = _password_PB.SecurePassword;
         if (securePassword.Length == 0)
         {
            return;
         }

         _setBusy(Strings.Msg_CheckingPasskey);
         try
         {
            // Materialize the managed copy while SecureString is still alive.
            // UseAsString zeroes the unmanaged BSTR before returning; LoginAsync
            // only needs the managed string, which outlives this call.
            string passkey = securePassword.UseAsString(static s => s);

            // Erase the PasswordBox buffer right after submitting so the secret
            // is not kept alive longer than necessary.
            _password_PB.Clear();

            _ = await database.LoginAsync(passkey).ConfigureAwait(true);
         }
         catch (CorruptedSourceException ex)
         {
            // Wrong passkeys stay soft (Login returns null). Corruption and other
            // hard failures bubble up so the user can be told and restart cleanly.
            Log.Error(ex, "Database corrupted during login");
            AppServices.Dialogs.Warn(
               Strings.Msg_CorruptedDatabase,
               Strings.Title_CorruptedDatabase);
            _resetCredentials();
            _endSession();
            return;
         }
         finally
         {
            _password_PB.Clear();
            _clearBusy();
         }

         if (_isClosing)
         {
            return;
         }

         if (_session.Database?.User is null)
         {
            // Incomplete or failed layer: stay on the password prompt so the next
            // passkey can be entered (or Escape to abandon). The idle timer is
            // restarted by the caller once this method returns.
            _ = _password_PB.Focus();
            return;
         }

         _session.ApplySessionLanguage();
         _session.ApplySessionTheme();

         Hide();
         _resetCredentials();

         bool stayOpen = UserServicesView.ShowUser(this);

         // ShowUser is modal: EndSession may have applied the app language/theme
         // while this window was still hidden under the dialog, so Loc bindings
         // can miss the refresh. Re-apply after the modal returns, then show the login UI.
         _restoreAppPreferences();
         _resetCredentials();

         if (!stayOpen)
         {
            Close();
         }
         else
         {
            Show();
         }
      }

      private static void _restoreAppPreferences()
      {
         // forceRefresh: EndSession may already have switched culture/theme while
         // MainWindow was hidden under the modal; Loc bindings still need a nudge.
         _ = LocalizationService.Apply(AppInfo.AppSettings.Language, forceRefresh: true);
         _ = ThemeService.Apply(AppInfo.AppSettings.Theme, forceRefresh: true);
      }

      private void _setBusy(string message)
      {
         // Open/Login must not race with the idle reset: pause the timer for the
         // whole await, then let the caller restart it if the attempt failed.
         _stopIdleTimer();
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

      private void _endSession(bool closeDatabase = true)
      {
         IDatabase? database = _session.Database;
         if (database is not null)
         {
            database.DatabaseClosed -= _database_DatabaseClosed;
            database.AutoSaveDetected -= _database_AutoSaveDetected;
         }

         _session.EndSession(closeDatabase);
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
               Strings.Msg_AutosaveDetected,
               Strings.Title_AutosaveDetected,
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
            // The database already closed itself; only clear the session reference.
            _endSession(closeDatabase: false);
            _restoreAppPreferences();
            Show();
         });
      }

      private void _resetCredentials()
      {
         _stopIdleTimer();

         _mainViewModel.IsAwaitingPasskeys = false;
         _mainViewModel.DatabaseFile = string.Empty;
         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Clear();
         _password_PB.Visibility = Visibility.Collapsed;
      }

      /// <summary>
      /// Starts or restarts the login idle countdown from
      /// <see cref="Models.AppSettings.LoginIdleTimeoutSeconds"/>. A value of
      /// <c>0</c> leaves the timer off (no reset, no title countdown).
      /// </summary>
      private void _armIdleTimer()
      {
         int timeout = Math.Max(0, AppInfo.AppSettings.LoginIdleTimeoutSeconds);
         if (timeout == 0)
         {
            _stopIdleTimer();
            return;
         }

         _idleSecondsRemaining = timeout;
         _refreshIdleTitle();
         _idleTimer.Stop();
         _idleTimer.Start();
      }

      private void _stopIdleTimer()
      {
         _idleTimer.Stop();
         _idleSecondsRemaining = 0;
         _mainViewModel.WindowTitle = MainViewModel.AppTitle;
      }

      private void _refreshIdleTitle()
      {
         _mainViewModel.WindowTitle = MainViewModel.AppTitle + Strings.Format(nameof(Strings.Title_IdleResetCredentialTimeout), _idleSecondsRemaining);
      }
   }
}
