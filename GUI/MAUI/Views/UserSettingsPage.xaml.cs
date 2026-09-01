using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class UserSettingsPage : ContentPage
   {
      public UserSettingsPage()
      {
         InitializeComponent();
         BindingContext = new UserSettingsViewModel();
      }
   }
}
