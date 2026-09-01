using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class LoginPage : ContentPage
   {
      private readonly LoginViewModel _viewModel = new();

      public LoginPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
         LocalizationService.LanguageChanged += (_, _) => _viewModel.RefreshLabels();
      }

      protected override void OnAppearing()
      {
         base.OnAppearing();
         _viewModel.RefreshLabels();
      }
   }
}
