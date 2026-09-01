using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class CreateUserPage : ContentPage
   {
      public CreateUserPage()
      {
         InitializeComponent();
         BindingContext = new CreateUserViewModel();
      }
   }
}
