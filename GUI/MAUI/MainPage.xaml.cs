using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Events;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class MainPage : ContentPage, IDisposable
   {
      private readonly LoginViewModel _viewModel = new();
#if WINDOWS
      private bool _commandLineApplied;
#endif

      public MainPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
         _configureSecretEntry();

         _viewModel.LoggedIn += _onLoggedIn;
         _viewModel.SessionEnded += _onSessionEnded;
         _viewModel.IdleTimeoutElapsed += (_, _) => CredentialEntry.Text = string.Empty;
         LocalizationService.LanguageChanged += (_, _) => _viewModel.OnLanguageChanged();

         if (!AppServices.Capabilities.SupportsKeyboardShortcuts)
         {
            ToolbarItems.AddLocalized(nameof(Strings.Menu_NewUser), _viewModel.NewUserCommand, ToolbarItemOrder.Secondary);
            ToolbarItems.AddLocalized(nameof(Strings.Menu_GenerateRandomPassword), _viewModel.GeneratePasswordCommand, ToolbarItemOrder.Secondary);
            ToolbarItems.AddLocalized(nameof(Strings.Menu_AppSettings), _viewModel.AppSettingsCommand, ToolbarItemOrder.Secondary);
         }

         if (AppServices.Lifecycle is { } life)
         {
            life.EnteredBackground += _onEnteredBackground;
         }
      }

#if WINDOWS
      protected override async void OnAppearing()
      {
         base.OnAppearing();
         if (_commandLineApplied)
         {
            return;
         }

         _commandLineApplied = true;
         await _viewModel.ApplyCommandLineAsync(Environment.GetCommandLineArgs()).ConfigureAwait(true);
      }
#endif

      public void Dispose()
      {
         if (AppServices.Lifecycle is { } life)
         {
            life.EnteredBackground -= _onEnteredBackground;
         }

         _viewModel.Dispose();
      }

      protected override void OnHandlerChanged()
      {
         base.OnHandlerChanged();
         _configureSecretEntry();
      }

      private void _configureSecretEntry()
      {
         _applySecretInputType(CredentialEntry);
      }

      private static void _applySecretInputType(Entry entry)
      {
#if ANDROID
         if (entry.Handler?.PlatformView is Android.Widget.EditText edit)
         {
            edit.InputType = Android.Text.InputTypes.ClassText
               | Android.Text.InputTypes.TextVariationPassword
               | Android.Text.InputTypes.TextFlagNoSuggestions;
         }
#else
         _ = entry;
#endif
      }

      private void _onEnteredBackground(object? sender, EventArgs e)
      {
         MainThread.BeginInvokeOnMainThread(() =>
         {
            CredentialEntry.Text = string.Empty;
            if (!_viewModel.IsLoggedIn)
            {
               // A half-open session must not survive in the background (no rollback).
               _viewModel.Reset();
            }
         });
      }

      private void _onCredentialTextChanged(object? sender, TextChangedEventArgs e)
         => _viewModel.NotifyCredentialInput(!string.IsNullOrEmpty(e.NewTextValue));

      private async void _onCredentialCompleted(object? sender, EventArgs e)
      {
         if (_viewModel.IsBusy)
         {
            return;
         }

         string value = CredentialEntry.Text ?? string.Empty;
         CredentialEntry.Text = string.Empty;

         if (!_viewModel.DatabaseOpened)
         {
            await _viewModel.SubmitUsernameAsync(value).ConfigureAwait(true);
         }
         else
         {
            _ = await _viewModel.SubmitPasskeyAsync(value).ConfigureAwait(true);
         }

         if (!_viewModel.IsLoggedIn)
         {
            _ = CredentialEntry.Focus();
         }
      }

      private void _onReset(object? sender, EventArgs e)
      {
         CredentialEntry.Text = string.Empty;
         _viewModel.Reset();
         _ = CredentialEntry.Focus();
      }

      private async void _onLoggedIn(object? sender, EventArgs e)
      {
         if (_viewModel.Database is not { } database)
         {
            return;
         }

         await Navigation.PushAsync(new Pages.Vault.VaultPage(database)).ConfigureAwait(true);
      }

      private async void _onSessionEnded(object? sender, LogoutEventArgs e)
      {
         if (Navigation.NavigationStack.Count > 1)
         {
            await Navigation.PopToRootAsync().ConfigureAwait(true);
         }

         if (e.LoginTimeoutReached)
         {
            await DisplayAlertAsync(Strings.Title_SessionClosed, Strings.Msg_SessionClosedTimeout, Strings.Button_OK)
               .ConfigureAwait(true);
         }
      }

      private async void _onPickVault(object? sender, EventArgs e)
      {
         if (_viewModel.IsBusy)
         {
            return;
         }

#if ANDROID
         await _pickSafVaultAsync().ConfigureAwait(true);
#else
         await _pickPathVaultAsync().ConfigureAwait(true);
#endif
         _ = CredentialEntry.Focus();
      }

#if ANDROID
      private async Task _pickSafVaultAsync()
      {
         (global::Android.Net.Uri Uri, string Name)? picked = await MainActivity.PickVaultDocumentAsync()
            .ConfigureAwait(true);
         if (picked is null)
         {
            return;
         }

         Platforms.Android.AndroidVaultSourceFactory.RegisterSafUri(picked.Value.Uri, picked.Value.Name);
         _viewModel.SelectVault(picked.Value.Uri.ToString()!, picked.Value.Name);
      }
#endif

      private async Task _pickPathVaultAsync()
      {
         FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
         {
            PickerTitle = Strings.Title_OpenDatabase,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
               { DevicePlatform.WinUI, [".pku"] },
            }),
         }).ConfigureAwait(true);

         if (file?.FullPath is not { Length: > 0 } path)
         {
            return;
         }

         _viewModel.SelectVault(path, Path.GetFileName(path));
      }
   }
}
