using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.UnitTests.Gui.Fakes;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   /// <summary>
   /// Wires <see cref="AppServices"/> to fakes for ViewModel tests.
   /// Production code never calls this.
   /// Serializes access because Reset restores the real WPF clipboard.
   /// </summary>
   internal static class GuiTestServices
   {
      private static readonly object _appServicesGate = new();
      private static bool _gateHeld;

      public static ClipboardManager Clipboard { get; private set; } = null!;

      public static FakeSessionService Session { get; private set; } = null!;

      public static FakeDialogService Dialogs { get; private set; } = null!;

      public static FakeNavigationService Navigation { get; private set; } = null!;

      public static void Install()
      {
         Monitor.Enter(_appServicesGate);
         _gateHeld = true;

         try
         {
            Clipboard = new ClipboardManager();
            Session = new FakeSessionService();
            Dialogs = new FakeDialogService();
            Navigation = new FakeNavigationService();

            AppServices.Clipboard = Clipboard;
            AppServices.Session = Session;
            AppServices.Dialogs = Dialogs;
            AppServices.Navigation = Navigation;
            AppServices.Cryptography = UnitTestsHelper.CryptographicCenter;
            AppServices.Serialization = UnitTestsHelper.SerializationCenter;
            AppServices.PasswordFactory = UnitTestsHelper.PasswordFactory;
         }
         catch
         {
            _releaseGate();
            throw;
         }
      }

      public static void Reset()
      {
         try
         {
            AppServices.Reset();
         }
         finally
         {
            _releaseGate();
         }
      }

      private static void _releaseGate()
      {
         if (!_gateHeld)
         {
            return;
         }

         _gateHeld = false;
         Monitor.Exit(_appServicesGate);
      }
   }
}
