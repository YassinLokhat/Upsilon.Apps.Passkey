using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Tools
{
   internal sealed partial class PasswordGeneratorPage : ContentPage, IResultPage<string>
   {
      private readonly PasswordGeneratorViewModel _viewModel = new();
      private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public PasswordGeneratorPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
         InsertButton.IsVisible = PasswordGeneratorViewModel.IsInsertAvailable;
         _viewModel.InsertRequested += (s, e) => _complete(_viewModel.GeneratedPassword);
      }

      public Task<string?> Completion => _completion.Task;

      protected override bool OnBackButtonPressed()
      {
         _complete(null);
         return true;
      }

      protected override void OnDisappearing()
      {
         base.OnDisappearing();
         _complete(null);
      }

      private void _onCancel(object? sender, EventArgs e) => _complete(null);

      private void _complete(string? result)
      {
         if (_completion.TrySetResult(result))
         {
            _viewModel.Close();
         }
      }
   }
}
