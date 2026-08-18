using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.UnitTests.Gui.Fakes;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   /// <summary>
   /// Wires <see cref="AppServices"/> to fakes for ViewModel tests.
   /// Production code never calls this.
   /// </summary>
   internal static class GuiTestServices
   {
      public static ClipboardManager Clipboard { get; private set; } = null!;

      public static FakeSessionService Session { get; private set; } = null!;

      public static FakeDialogService Dialogs { get; private set; } = null!;

      public static FakeNavigationService Navigation { get; private set; } = null!;

      public static void Install()
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

      public static void Reset() => AppServices.Reset();
   }
}
