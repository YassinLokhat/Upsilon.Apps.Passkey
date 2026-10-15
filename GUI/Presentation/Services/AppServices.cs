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

      public static IAppInfo Info { get; set; } = new AssemblyAppInfo(typeof(AppServices).Assembly);

      public static IAppLog Log { get; set; } = new NullAppLog();

      public static INavigationService Navigation { get; set; } = new NavigationService();

      public static IUiDispatcher? Ui { get; set; }

      public static IUiTimerFactory? Timers { get; set; }

      public static IAppPaths? Paths { get; set; }

      public static IAppSettingsStore? Settings { get; set; }

      /// <summary>
      /// Folder where username-named vaults are looked up and created: the configured
      /// <see cref="Models.AppSettings.DefaultDatabaseDirectory"/>, else the host default.
      /// <c>null</c> when the host registered no <see cref="Paths"/>.
      /// </summary>
      public static string? DefaultVaultDirectory
      {
         get
         {
            if (Paths is not { } paths)
            {
               return null;
            }

            string? configured = Settings?.Current.DefaultDatabaseDirectory;
            return string.IsNullOrWhiteSpace(configured) ? paths.DefaultDatabaseDirectory : configured;
         }
      }

      public static IHostCapabilities Capabilities { get; set; } = HostCapabilities.Desktop;

      public static IThemePlatform? Theme { get; set; }

      public static ILauncher? Launcher { get; set; }

      public static IAppLifecycle? Lifecycle { get; set; }

      public static IVaultSourceFactory? VaultSources { get; set; }

      public static IClipboardService? Clipboard { get; set; }

      /// <summary>Defaults to the dialog path pickers when unset.</summary>
      public static IFileExchange? Files { get; set; }

      public static IConflictCopyStore? ConflictCopies { get; set; }

      /// <summary>Single runner for manual and automatic <c>.pkbf</c> builds; idle on hosts without the offline filter.</summary>
      public static OfflineLeakFilterUpdateService OfflineLeakFilterUpdate { get; set; } = new();

      public static ISessionService Session { get; set; } = new SessionService();

      public static ICryptographyCenter Cryptography { get; set; } = new CryptographyCenter();

      public static ISerializationCenter Serialization { get; set; } = new JsonSerializationCenter();

      public static IPasswordFactory PasswordFactory { get; set; } = new PasswordFactory();

      public static ISecretMemoryProtector SecretProtector { get; set; } = new SecretMemoryProtector();

      /// <summary>Restores library defaults and clears host ports. Test cleanup only.</summary>
      public static void Reset()
      {
         Dialogs = null;
         Info = new AssemblyAppInfo(typeof(AppServices).Assembly);
         Log = new NullAppLog();
         Navigation = new NavigationService();
         Ui = null;
         Timers = null;
         Paths = null;
         Settings = null;
         Capabilities = HostCapabilities.Desktop;
         Theme = null;
         Launcher = null;
         Lifecycle = null;
         VaultSources = null;
         Clipboard = null;
         Files = null;
         ConflictCopies = null;
         OfflineLeakFilterUpdate = new OfflineLeakFilterUpdateService();
         Session = new SessionService();
         Cryptography = new CryptographyCenter();
         Serialization = new JsonSerializationCenter();
         PasswordFactory = new PasswordFactory();
         SecretProtector = new SecretMemoryProtector();
      }
   }
}
