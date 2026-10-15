using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Composition root locator shared by hosts. Hosts assign platform ports at
   /// startup; tests replace properties then call <see cref="Reset"/>.
   /// </summary>
   internal static class AppServices
   {
      public static IDialogService? Dialogs { get; set; }

      public static INavigationService Navigation { get; set; } = new NavigationService();

      public static IUiDispatcher? Ui { get; set; }

      public static IUiTimerFactory? Timers { get; set; }

      public static IAppPaths? Paths { get; set; }

      public static IThemePlatform? Theme { get; set; }

      public static ILauncher? Launcher { get; set; }

      public static IAppLifecycle? Lifecycle { get; set; }

      public static IVaultSourceFactory? VaultSources { get; set; }

      public static IClipboardService? Clipboard { get; set; }

      public static ICryptographyCenter Cryptography { get; set; } = new CryptographyCenter();

      public static ISerializationCenter Serialization { get; set; } = new JsonSerializationCenter();

      public static IPasswordFactory PasswordFactory { get; set; } = new PasswordFactory();

      public static ISecretMemoryProtector SecretProtector { get; set; } = new SecretMemoryProtector();

      /// <summary>Restores library defaults and clears host ports. Test cleanup only.</summary>
      public static void Reset()
      {
         Dialogs = null;
         Navigation = new NavigationService();
         Ui = null;
         Timers = null;
         Paths = null;
         Theme = null;
         Launcher = null;
         Lifecycle = null;
         VaultSources = null;
         Clipboard = null;
         Cryptography = new CryptographyCenter();
         Serialization = new JsonSerializationCenter();
         PasswordFactory = new PasswordFactory();
         SecretProtector = new SecretMemoryProtector();
      }
   }
}
