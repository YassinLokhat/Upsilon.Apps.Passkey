using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Events;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class MainPage : ContentPage
   {
      private readonly LoginViewModel _viewModel = new();

      public MainPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
         _configureSecretEntry();

         _viewModel.LoggedIn += _onLoggedIn;
         _viewModel.SessionEnded += _onSessionEnded;

         if (AppServices.Lifecycle is { } life)
         {
            life.EnteredBackground += _onEnteredBackground;
         }
      }

      protected override void OnHandlerChanged()
      {
         base.OnHandlerChanged();
         _configureSecretEntry();
      }

      private void _configureSecretEntry()
      {
         _applyAutofillGuards(CredentialEntry);
      }

      private static void _applyAutofillGuards(Entry entry)
      {
#if ANDROID
         if (entry.Handler?.PlatformView is Android.Widget.EditText edit)
         {
            edit.ImportantForAutofill = Android.Views.ImportantForAutofill.NoExcludeDescendants;
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

         await Navigation.PushAsync(new SessionPage(new SessionViewModel(database))).ConfigureAwait(true);
      }

      private async void _onSessionEnded(object? sender, LogoutEventArgs e)
      {
         if (Navigation.NavigationStack.Count > 1)
         {
            await Navigation.PopToRootAsync().ConfigureAwait(true);
         }

         if (e.LoginTimeoutReached)
         {
            await DisplayAlertAsync("Session closed", "The session was closed after the configured inactivity timeout.", "OK")
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
            PickerTitle = "Open Passkey vault",
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
