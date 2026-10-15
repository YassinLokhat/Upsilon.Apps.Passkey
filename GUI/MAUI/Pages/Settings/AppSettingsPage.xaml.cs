using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Settings
{
   [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The ViewModel is disposed when the modal completes; pages are never reused.")]
   internal sealed partial class AppSettingsPage : ContentPage, IResultPage<bool>
   {
      private readonly AppSettingsViewModel _viewModel;
      private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

      public AppSettingsPage()
      {
         InitializeComponent();
         BindingContext = _viewModel = new AppSettingsViewModel();
         _viewModel.CloseRequested += _onCloseRequested;
      }

      public Task<bool> Completion => _completion.Task;

      protected override bool OnBackButtonPressed()
      {
         _complete(false);
         return true;
      }

      private void _onClose(object? sender, EventArgs e) => _complete(false);

      private void _onCloseRequested(object? sender, EventArgs e) => _complete(true);

      private void _onEnabledToggled(object? sender, ToggledEventArgs e)
         => _viewModel.ApplyOfflineLeakFilterEnabled(e.Value);

      private void _complete(bool result)
      {
         if (!_completion.TrySetResult(result))
         {
            return;
         }

         // Closing the page never cancels a background build; only Cancel or app exit does.
         _viewModel.CloseRequested -= _onCloseRequested;
         _viewModel.Dispose();
      }
   }
}
