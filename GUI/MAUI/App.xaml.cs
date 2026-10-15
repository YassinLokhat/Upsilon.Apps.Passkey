using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal sealed partial class App : Application
   {
      private readonly MauiAppLifecycle _lifecycle = new();

      public App()
      {
         InitializeComponent();
         AppServices.Lifecycle = _lifecycle;
      }

      protected override Window CreateWindow(IActivationState? activationState)
      {
         return new Window(new AppShell());
      }

      protected override void OnSleep()
      {
         base.OnSleep();
#if ANDROID
         // On Windows OnSleep also fires on minimize; the WPF client keeps the
         // half-open login and the clipboard countdown in that case.
         AppServices.Clipboard?.ClearIfStillOwned();
         _lifecycle.NotifyEnteredBackground();
#endif
      }

      protected override void OnResume()
      {
         base.OnResume();
         _lifecycle.NotifyResumed();
      }
   }
}
