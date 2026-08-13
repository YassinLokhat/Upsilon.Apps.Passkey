using System.IO;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Core.Utils.LeakFilter;
using Upsilon.Apps.Passkey.GUI.WPF.OSSpecific;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// Minimalist service locator. Provides single instances of every cross-cutting
   /// service so view-models can grab them without taking on a third-party DI
   /// container. The factories below are intentionally inline because there is
   /// only one composition root (the WPF process) and no extra dependency is
   /// allowed.
   /// </summary>
   internal static class AppServices
   {
      // Must initialize before PasswordFactory so ReloadLocalFilter sees the
      // exe-adjacent leak-filter folder rather than LocalAppData.
      private static readonly string _leakFilterRoot = _configureLeakFilterRoot();

      public static IDialogService Dialogs { get; } = new DialogService();

      public static ISessionService Session { get; } = new SessionService();

      public static INavigationService Navigation { get; } = new NavigationService();

      public static ICryptographyCenter Cryptography { get; } = new CryptographyCenter();

      public static ISerializationCenter Serialization { get; } = new JsonSerializationCenter();

      public static IPasswordFactory PasswordFactory { get; } = new PasswordFactory();

      public static IClipboardManager Clipboard { get; } = new ClipboardManager();

      /// <summary>
      /// Absolute path of the offline leak-filter directory next to the executable.
      /// </summary>
      public static string LeakFilterRoot => _leakFilterRoot;

      private static string _configureLeakFilterRoot()
      {
         string exeDir = Path.GetDirectoryName(Environment.ProcessPath)
            ?? AppContext.BaseDirectory;
         string root = Path.Combine(exeDir, "leak-filter");
         LeakFilterPaths.SetRootDirectory(root);
         return LeakFilterPaths.RootDirectory;
      }
   }
}
