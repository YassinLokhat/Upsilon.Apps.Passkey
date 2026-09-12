namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class App : Application
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