using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   public partial class AppSettingsPage : ContentPage
   {
      public AppSettingsPage()
      {
         InitializeComponent();
         BindingContext = new AppSettingsViewModel();
      }
   }
}
