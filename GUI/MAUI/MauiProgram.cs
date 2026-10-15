using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI
{
   internal static class MauiProgram
   {
      public static MauiApp CreateMauiApp()
      {
         // Touch so CA1812 sees framework-created types as used (factories are never invoked).
         _ = MauiFrameworkCreatedTypeAnchors.Factories.Length;

         MauiAppPaths paths = new();
         AppServices.Info = new AssemblyAppInfo(typeof(MauiProgram).Assembly);
         AppServices.Log = new TraceFileAppLog(paths.LogDirectory);
         AppServices.Ui = new MauiUiDispatcher();
         AppServices.Timers = new MauiUiTimerFactory();
         AppServices.Paths = paths;
         AppServices.Settings = new JsonAppSettingsStore(paths);
#if ANDROID
         AppServices.Capabilities = HostCapabilities.Mobile;
#else
         AppServices.Capabilities = HostCapabilities.Desktop with { SupportsMultipleWindows = false };
#endif
         AppServices.Launcher = new MauiLauncher();
         AppServices.Dialogs = new MauiDialogService();
         AppServices.Theme = new MauiThemePlatform();
#if WINDOWS
         AppServices.Clipboard = new Platforms.Windows.WindowsClipboardService();
         AppServices.VaultSources = new PathVaultSourceFactory();
#elif ANDROID
         AppServices.Clipboard = new Platforms.Android.AndroidClipboardService();
         AppServices.VaultSources = new Platforms.Android.AndroidVaultSourceFactory();
#else
         AppServices.Clipboard = new MauiClipboardService();
         AppServices.VaultSources = new PathVaultSourceFactory();
#endif

         MauiAppBuilder builder = MauiApp.CreateBuilder();
         builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
               fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

         return builder.Build();
      }
   }
}
