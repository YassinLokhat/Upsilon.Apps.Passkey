using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   public partial class App : Application
   {
      public App()
      {
         InitializeComponent();

         _ = PasskeyAppInfo.ConfigFile;
         _ = LocalizationService.Apply(PasskeyAppInfo.AppSettings.Language);
         _ = ThemeService.Apply(PasskeyAppInfo.AppSettings.Theme);

         Log.Info($"Application starting (PID {Environment.ProcessId}).");
      }

      protected override Window CreateWindow(IActivationState? activationState)
      {
         Window window = new(new AppShell());
         window.Destroying += (_, _) =>
         {
            AppServices.Session.EndSession();
            Log.Info("Application exiting.");
            Log.Flush();
         };
         return window;
      }
   }
}
