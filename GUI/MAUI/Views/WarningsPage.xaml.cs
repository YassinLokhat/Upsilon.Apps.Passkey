using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class WarningsPage : ContentPage
   {
      private readonly WarningsViewModel _viewModel = new();

      public WarningsPage()
      {
         InitializeComponent();
         BindingContext = _viewModel;
      }

      protected override void OnAppearing()
      {
         base.OnAppearing();
         _viewModel.Refresh();
      }
   }
}
