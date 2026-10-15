using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class SessionPage : ContentPage
   {
      private readonly SessionViewModel _viewModel;

      public SessionPage(SessionViewModel viewModel)
      {
         ArgumentNullException.ThrowIfNull(viewModel);
         InitializeComponent();
         _viewModel = viewModel;
         BindingContext = viewModel;

         // Leaving the session goes through Log out (or auto-logout), never a bare back navigation.
         Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false, IsEnabled = false });
      }

      protected override bool OnBackButtonPressed()
      {
         _viewModel.Logout();
         return true;
      }

      private void _onLogout(object? sender, EventArgs e)
         => _viewModel.Logout();
   }
}
