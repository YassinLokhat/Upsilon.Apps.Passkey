using Microsoft.Extensions.DependencyInjection;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   public partial class App : Application
   {
      public App()
      {
         InitializeComponent();
      }

      protected override Window CreateWindow(IActivationState? activationState)
      {
         return new Window(new AppShell());
      }
   }
}