using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class PasswordGeneratorPage : ContentPage
   {
      public PasswordGeneratorPage()
      {
         InitializeComponent();
         BindingContext = new PasswordGeneratorViewModel();
      }
   }
}
