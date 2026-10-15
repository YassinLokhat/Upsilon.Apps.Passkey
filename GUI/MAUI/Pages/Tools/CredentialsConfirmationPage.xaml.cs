using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Tools
{
   internal sealed partial class CredentialsConfirmationPage : ContentPage, IResultPage<bool>
   {
      private readonly CredentialsConfirmationViewModel _viewModel;
      private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public CredentialsConfirmationPage(IEnumerable<string> credentials, bool isNew)
      {
         InitializeComponent();
         BindingContext = _viewModel = new CredentialsConfirmationViewModel(credentials, isNew);
      }

      public Task<bool> Completion => _completion.Task;

      protected override void OnAppearing()
      {
         base.OnAppearing();
         _ = CredentialEntry.Focus();
      }

      protected override bool OnBackButtonPressed()
      {
         _complete(false);
         return true;
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         _complete(false);
      }

      private void _onCompleted(object? sender, EventArgs e)
      {
         string text = CredentialEntry.Text ?? string.Empty;
         CredentialEntry.Text = string.Empty;
         if (text.Length == 0)
         {
            return;
         }

         if (_viewModel.ValidateCredentials(text))
         {
            _complete(true);
         }
      }

      private void _onReset(object? sender, EventArgs e)
      {
         _viewModel.ClearCredentials();
         CredentialEntry.Text = string.Empty;
         _ = CredentialEntry.Focus();
      }

      private void _onCancel(object? sender, EventArgs e) => _complete(false);

      private void _complete(bool result)
      {
         CredentialEntry.Text = string.Empty;
         _viewModel.ReleaseExpectedCredentials();
         _ = _completion.TrySetResult(result);
      }
   }
}
