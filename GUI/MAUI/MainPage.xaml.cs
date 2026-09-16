using Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class MainPage : ContentPage
   {
      public MainPage()
      {
         InitializeComponent();

         BindingContext = new MainPageViewModel();
      }
   }
}
