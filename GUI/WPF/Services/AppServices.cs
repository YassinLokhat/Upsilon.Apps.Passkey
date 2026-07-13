using Upsilon.Apps.Passkey.Core.Utils;
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
      public static IDialogService Dialogs { get; } = new DialogService();

      public static ISessionService Session { get; } = new SessionService();

      public static INavigationService Navigation { get; } = new NavigationService();

      public static ICryptographyCenter Cryptography { get; } = new CryptographyCenter();

      public static ISerializationCenter Serialization { get; } = new JsonSerializationCenter();

      public static IPasswordFactory PasswordFactory { get; } = new PasswordFactory();

      public static IClipboardManager Clipboard { get; } = new ClipboardManager();
   }
}
