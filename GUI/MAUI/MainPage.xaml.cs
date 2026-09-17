using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class MainPage : ContentPage
   {
      private MainPageViewModel _viewModel;

      public MainPage()
      {
         InitializeComponent();

         BindingContext = _viewModel = new MainPageViewModel();
      }

      private void _credential_Entry_Completed(object? sender, EventArgs e)
      {
         _viewModel.ActualCredential = string.Empty;
         _viewModel.DatabaseOpened = true;
      }

      private void _resetCredentials_Button_Clicked(object? sender, EventArgs e)
      {
         _viewModel.ActualCredential = string.Empty;
         _viewModel.DatabaseOpened = false;
      }
   }
}
