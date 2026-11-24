using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.Adapters;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Models;

using AppServices = Upsilon.Apps.Passkey.GUI.WPF.Services.AppServices;

namespace Upsilon.Apps.Passkey.GUI.WPF
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml. The progressive login itself lives in
   /// the shared <see cref="LoginViewModel"/>; this window keeps focus, the passkey
   /// read, the vault modal and the close-while-busy prompt.
   /// </summary>
   internal sealed partial class MainWindow : Window, ILanguageAware, IDisposable
   {
      private readonly LoginViewModel _viewModel;
      private readonly PasswordBoxSecretInput _passkeyInput;

      private bool _isClosing;
      private bool _disposed;

      // Close-while-busy: prompt once (abort, continue headless, or stay open).
      private bool _forceClose;
      private bool _exitPromptActive;
      private bool _awaitingLeakFilterExit;

      public MainWindow()
      {
         InitializeComponent();
         _passkeyInput = new PasswordBoxSecretInput(_password_PB);

         DataContext = _viewModel = new LoginViewModel();
         _viewModel.CredentialsReset += _viewModel_CredentialsReset;
         _viewModel.ErrorReported += _viewModel_ErrorReported;
         _viewModel.PropertyChanged += _viewModel_PropertyChanged;

         _username_TB.KeyUp += _credential_KeyUp;
         _password_PB.KeyUp += _credential_KeyUp;
         Loaded += _mainWindow_Loaded;
         Closing += _window_Closing;
         Closed += _window_Closed;
      }

      public void OnLanguageChanged() => this.ForwardToDataContext();

      private async void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         this.PostLoadSetup();
         _focusCredential();

         if (AppInfo.TryConsumeConfigLoadError())
         {
            AppServices.Dialogs.Warn(Strings.Msg_ConfigFileError, Strings.Title_ConfigFileError);
         }

         string[] args = Environment.GetCommandLineArgs();
         await _viewModel.ApplyCommandLineAsync(args).ConfigureAwait(true);

         // Like a typed username, a failed one from the command line stays in the box.
         if (!_viewModel.DatabaseOpened && args.Length > 2 && args[2] is { Length: > 0 } username)
         {
            _username_TB.Text = username;
            _username_TB.CaretIndex = username.Length;
         }

         _afterSubmit();
      }

      private void _window_Closing(object? sender, CancelEventArgs e)
      {
         if (_forceClose)
         {
            return;
         }

         // Keep the hidden window until the job finishes and Shutdown runs.
         if (_awaitingLeakFilterExit)
         {
            if (AppServices.OfflineLeakFilterUpdate.IsBusy)
            {
               e.Cancel = true;
            }

            return;
         }

         if (_exitPromptActive)
         {
            e.Cancel = true;
            return;
         }

         if (!AppServices.OfflineLeakFilterUpdate.IsBusy)
         {
            return;
         }

         // Vault window already handled Yes/No.
         if (AppServices.OfflineLeakFilterUpdate.SkipClosePrompt)
         {
            AppServices.OfflineLeakFilterUpdate.SkipClosePrompt = false;
            return;
         }

         e.Cancel = true;
         _exitPromptActive = true;

         try
         {
            // Vault close may leave this window hidden; show so the prompt has an owner.
            if (!IsVisible)
            {
               Show();
            }

            if (!AppServices.OfflineLeakFilterUpdate.IsBusy)
            {
               _forceClose = true;
               Close();
               return;
            }

            MessageBoxResult result = AppServices.Dialogs.Confirm(
               Strings.Msg_OfflineLeakUpdateExitPrompt,
               Strings.Title_OfflineLeakUpdateInProgress,
               MessageBoxButton.YesNoCancel,
               MessageBoxImage.Question);

            switch (result)
            {
               case MessageBoxResult.Yes:
                  _continueLeakFilterInBackground();
                  break;

               case MessageBoxResult.No:
                  AppServices.OfflineLeakFilterUpdate.Cancel();
                  _forceClose = true;
                  Close();
                  break;

               default:
                  break;
            }
         }
         finally
         {
            _exitPromptActive = false;
         }
      }

      private void _continueLeakFilterInBackground()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         if (!update.IsBusy)
         {
            _forceClose = true;
            Close();
            return;
         }

         _awaitingLeakFilterExit = true;
         update.ContinueThroughExit = true;

         // EndSession before Hide so vault/clipboard are gone before further filter I/O.
         _isClosing = true;
         _viewModel.Reset();
         AppServices.Session.EndSession();

         List<Window> others = [];
         foreach (Window window in Application.Current.Windows)
         {
            if (!ReferenceEquals(window, this))
            {
               others.Add(window);
            }
         }

         foreach (Window window in others)
         {
            try
            {
               window.Close();
            }
            catch (InvalidOperationException)
            {
               // Already closing / dispatcher shutting down.
            }
         }

         Hide();
         Log.Info("Offline leak filter: continuing update in background after UI close.");

         void onBusyChanged(object? sender, EventArgs args)
         {
            if (update.IsBusy)
            {
               return;
            }

            update.BusyChanged -= onBusyChanged;
            _ = Dispatcher.BeginInvoke(() =>
            {
               if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
               {
                  return;
               }

               Application.Current?.Shutdown();
            });
         }

         update.BusyChanged += onBusyChanged;
         if (!update.IsBusy)
         {
            onBusyChanged(update, EventArgs.Empty);
         }
      }

      private void _window_Closed(object? sender, EventArgs e) => Dispose();

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         _isClosing = true;
         _viewModel.CredentialsReset -= _viewModel_CredentialsReset;
         _viewModel.ErrorReported -= _viewModel_ErrorReported;
         _viewModel.PropertyChanged -= _viewModel_PropertyChanged;
         _clearCredentials();

         // Closes a half-open vault; an open or a login still running is closed by the ViewModel.
         _viewModel.Reset();
         _viewModel.Dispose();
         AppServices.Session.EndSession();
      }

      private async void _credential_KeyUp(object sender, KeyEventArgs e)
      {
         // A second Enter would race the progressive stack, and Escape would
         // close a vault that the running open is about to publish.
         if (_viewModel.IsBusy)
         {
            return;
         }

         if (e.Key == Key.Enter)
         {
            if (ReferenceEquals(sender, _username_TB))
            {
               await _viewModel.SubmitUsernameAsync(_username_TB.Text).ConfigureAwait(true);
            }
            else if (await _submitPasskeyAsync().ConfigureAwait(true))
            {
               // Shown from here, not from LoggedIn, so the modal never nests inside the ViewModel call.
               _showVault();
               return;
            }

            _afterSubmit();
         }
         else if (e.Key == Key.Escape)
         {
            _viewModel.Reset();
         }
         else
         {
            _viewModel.NotifyCredentialInput(_username_TB.Text.Length != 0);
         }
      }

      private Task<bool> _submitPasskeyAsync()
      {
         string passkey = _passkeyInput.Read();
         _passkeyInput.Clear();
         return _viewModel.SubmitPasskeyAsync(passkey);
      }

      private void _afterSubmit()
      {
         if (_isClosing)
         {
            return;
         }

         if (_viewModel.DatabaseOpened)
         {
            _username_TB.Clear();
         }

         _viewModel.NotifyCredentialInput(_username_TB.Text.Length != 0);
         _focusCredential();
      }

      private void _showVault()
      {
         AppServices.Session.ApplySessionLanguage();
         AppServices.Session.ApplySessionTheme();

         Hide();
         _clearCredentials();

         bool stayOpen = UserServicesView.ShowUser();

         // ShowUser is modal: EndSession may have applied the app language/theme
         // while this window was still hidden under the dialog, so Loc bindings
         // can miss the refresh. Re-apply after the modal returns, then show the login UI.
         _restoreAppPreferences();
         _viewModel.Reset();

         if (stayOpen)
         {
            Show();
            return;
         }

         // Vault already prompted; finish headless or quit without re-asking.
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         if (update.ContinueThroughExit)
         {
            update.SkipClosePrompt = false;
            if (update.IsBusy)
            {
               _continueLeakFilterInBackground();
            }
            else
            {
               _forceClose = true;
               Close();
            }

            return;
         }

         Close();
      }

      private static void _restoreAppPreferences()
      {
         // forceRefresh: EndSession may already have switched culture/theme while
         // MainWindow was hidden under the modal; Loc bindings still need a nudge.
         // While a vault session is still attached, honour the user's
         // language/theme override — never stomp it with app-only Apply.
         IUser? user = AppServices.Session.User;
         _ = LocalizationService.ApplyEffective(
            AppInfo.AppSettings.Language,
            user?.Settings.Language,
            forceRefresh: true);
         _ = ThemeService.ApplyEffective(
            AppInfo.AppSettings.Theme,
            user?.Settings.Theme,
            forceRefresh: true);
      }

      private void _viewModel_CredentialsReset(object? sender, EventArgs e)
      {
         _clearCredentials();
         _focusCredential();
      }

      private void _viewModel_ErrorReported(object? sender, LoginErrorEventArgs e)
         => AppServices.Dialogs.Warn(e.Message, e.Title);

      private void _viewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == nameof(LoginViewModel.IsBusy))
         {
            this.SetIsBusy(_viewModel.IsBusy);
         }
      }

      private void _clearCredentials()
      {
         _username_TB.Clear();
         _passkeyInput.Clear();
      }

      private void _focusCredential()
      {
         if (_isClosing)
         {
            return;
         }

         _ = _viewModel.DatabaseOpened ? _password_PB.Focus() : _username_TB.Focus();
      }
   }
}
