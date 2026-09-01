using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class ActivitiesPage : ContentPage
   {
      private readonly ActivitiesViewModel _viewModel = new();

      public ActivitiesPage()
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
