using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;

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
               _viewModel.Reset();
            }
         });
      }

      private async void _onCredentialCompleted(object? sender, EventArgs e)
      {
         string value = CredentialEntry.Text ?? string.Empty;
         CredentialEntry.Text = string.Empty;

         if (!_viewModel.DatabaseOpened)
         {
            _viewModel.Username = value.Trim();
            await _viewModel.OpenByUsernameAsync().ConfigureAwait(true);
            return;
         }

         _ = await _viewModel.SubmitPasskeyAsync(value).ConfigureAwait(true);
      }

      private async void _onOpenByUsername(object? sender, EventArgs e)
      {
         if (string.IsNullOrWhiteSpace(_viewModel.Username)
             && !string.IsNullOrWhiteSpace(CredentialEntry.Text)
             && !_viewModel.DatabaseOpened)
         {
            _viewModel.Username = CredentialEntry.Text.Trim();
            CredentialEntry.Text = string.Empty;
         }

         await _viewModel.OpenByUsernameAsync().ConfigureAwait(true);
      }

      private void _onReset(object? sender, EventArgs e)
      {
         CredentialEntry.Text = string.Empty;
         _viewModel.Reset();
      }

      private async void _onPickVault(object? sender, EventArgs e)
      {
         if (string.IsNullOrWhiteSpace(_viewModel.Username))
         {
            await DisplayAlertAsync("Username required", "Type the vault username first.", "OK").ConfigureAwait(true);
            return;
         }

#if ANDROID
         await _pickSafVaultAsync().ConfigureAwait(true);
#else
         await _pickPathVaultAsync().ConfigureAwait(true);
#endif
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
         await _viewModel.OpenPathAsync(picked.Value.Uri.ToString()!).ConfigureAwait(true);
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

         await _viewModel.OpenPathAsync(path).ConfigureAwait(true);
      }
   }
}
