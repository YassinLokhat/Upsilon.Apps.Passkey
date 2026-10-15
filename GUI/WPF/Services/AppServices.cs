using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.Utils;
using SharedServices = Upsilon.Apps.Passkey.GUI.Presentation.Services.AppServices;

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
      private static IWindowDialogService _dialogs = _shareDialogs(new DialogService());

      /// <summary>
      /// Assigning also publishes the instance as the shared async dialog
      /// service when it implements it, so both locators show the same dialogs.
      /// </summary>
      public static IWindowDialogService Dialogs
      {
         get => _dialogs;
         set => _dialogs = _shareDialogs(value);
      }

      public static IDialogService AsyncDialogs
         => SharedServices.Dialogs ?? throw new InvalidOperationException("No dialog service is registered.");

      public static ISessionService Session
      {
         get => SharedServices.Session;
         set => SharedServices.Session = value;
      }

      public static INavigationService Navigation
      {
         get => SharedServices.Navigation;
         set => SharedServices.Navigation = value;
      }

      // The library services below are shared with GUI/Presentation so both
      // locators always hand out the same instances.
      public static ICryptographyCenter Cryptography
      {
         get => SharedServices.Cryptography;
         set => SharedServices.Cryptography = value;
      }

      public static ISerializationCenter Serialization
      {
         get => SharedServices.Serialization;
         set => SharedServices.Serialization = value;
      }

      // Created parameterless by the shared locator: AppInfo.AppSettings is still
      // the default instance at that point (config deserialize triggers it).
      // AppInfo attaches the real .pkbf after config.json is loaded.
      public static IPasswordFactory PasswordFactory
      {
         get => SharedServices.PasswordFactory;
         set => SharedServices.PasswordFactory = value;
      }

      public static IClipboardService Clipboard
      {
         get => SharedServices.Clipboard ??= new ClipboardManager();
         set => SharedServices.Clipboard = value;
      }

      public static ISecretMemoryProtector SecretProtector
      {
         get => SharedServices.SecretProtector;
         set => SharedServices.SecretProtector = value;
      }

      /// <summary>
      /// Shared offline leak-filter build/update runner (manual settings + startup auto-update).
      /// </summary>
      public static OfflineLeakFilterUpdateService OfflineLeakFilterUpdate { get; set; } = new();

      /// <summary>
      /// Restores production defaults. Intended for unit-test cleanup only.
      /// </summary>
      internal static void Reset()
      {
         Dialogs = new DialogService();
         Session = new SessionService(() => OfflineLeakFilterUpdate.IsBusy);
         Navigation = new NavigationService();
         Cryptography = new CryptographyCenter();
         Serialization = new JsonSerializationCenter();
         PasswordFactory = new PasswordFactory(AppInfo.AppSettings.LeakFilterConfig);
         Clipboard = new ClipboardManager();
         SecretProtector = new SecretMemoryProtector();
         OfflineLeakFilterUpdate = new OfflineLeakFilterUpdateService();
      }

      private static IWindowDialogService _shareDialogs(IWindowDialogService dialogs)
      {
         ArgumentNullException.ThrowIfNull(dialogs);
         if (dialogs is IDialogService shared)
         {
            SharedServices.Dialogs = shared;
         }

         return dialogs;
      }
   }
}
