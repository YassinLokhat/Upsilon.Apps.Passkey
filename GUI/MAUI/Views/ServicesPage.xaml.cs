using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class ServicesPage : ContentPage
   {
      private readonly ServicesViewModel _viewModel = new();

      public ServicesPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
      }

      protected override void OnAppearing()
      {
         base.OnAppearing();
         _viewModel.Load();
         _viewModel.TryConsumeInsertedIdentifier();
         _viewModel.RefreshSelected();
#if WINDOWS
         HotkeyService.Register(
            () => _viewModel.SelectedIdentifierForHotkey,
            () => _viewModel.SelectedPasswordForHotkey);
#endif
      }

      protected override void OnDisappearing()
      {
#if WINDOWS
         HotkeyService.Unregister();
#endif
         _viewModel.Unload();
         base.OnDisappearing();
      }
   }
}
