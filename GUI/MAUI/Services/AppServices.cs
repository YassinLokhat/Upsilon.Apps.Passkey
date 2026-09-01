using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Services
{
   /// <summary>
   /// Minimalist service locator matching the WPF client pattern.
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

      internal static void Reset()
      {
         Dialogs = new DialogService();
         Session = new SessionService();
         Navigation = new NavigationService();
         Cryptography = new CryptographyCenter();
         Serialization = new JsonSerializationCenter();
         PasswordFactory = new PasswordFactory(PasskeyAppInfo.AppSettings.LeakFilterConfig);
         Clipboard = new ClipboardManager();
      }
   }
}
