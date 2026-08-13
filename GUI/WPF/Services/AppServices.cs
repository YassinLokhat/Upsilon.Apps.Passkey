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
   /// <para>
   /// Properties are settable so unit tests can swap fakes. Production code must
   /// never assign them; call <see cref="Reset"/> from test cleanup only.
   /// </para>
   /// </summary>
   internal static class AppServices
   {
      public static IDialogService Dialogs { get; set; } = new DialogService();

      public static ISessionService Session { get; set; } = new SessionService();

      public static INavigationService Navigation { get; set; } = new NavigationService();

      public static ICryptographyCenter Cryptography { get; set; } = new CryptographyCenter();

      public static ISerializationCenter Serialization { get; set; } = new JsonSerializationCenter();

      public static IPasswordFactory PasswordFactory { get; set; } = new PasswordFactory();

      public static IClipboardManager Clipboard { get; set; } = new ClipboardManager();

      /// <summary>
      /// Restores production defaults. Intended for unit-test cleanup only.
      /// </summary>
      internal static void Reset()
      {
         Dialogs = new DialogService();
         Session = new SessionService();
         Navigation = new NavigationService();
         Cryptography = new CryptographyCenter();
         Serialization = new JsonSerializationCenter();
         PasswordFactory = new PasswordFactory();
         Clipboard = new ClipboardManager();
      }
   }
}
