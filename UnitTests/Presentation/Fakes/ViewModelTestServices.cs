using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation.Fakes
{
   /// <summary>
   /// Wires <see cref="AppServices"/> for ViewModel tests: inline dispatcher,
   /// manual timers, recording clipboard and dialogs, real session service.
   /// </summary>
   internal static class ViewModelTestServices
   {
      public static FakeClipboardService Clipboard { get; private set; } = new();

      public static FakeDialogService Dialogs { get; private set; } = new();

      public static void Install()
      {
         AppServices.Reset();
         Clipboard = new FakeClipboardService();
         Dialogs = new FakeDialogService();
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Timers = new FakeTimerFactory();
         AppServices.Clipboard = Clipboard;
         AppServices.Dialogs = Dialogs;
         AppServices.Cryptography = UnitTestsHelper.CryptographyCenter;
         AppServices.Serialization = UnitTestsHelper.SerializationCenter;
         AppServices.PasswordFactory = UnitTestsHelper.PasswordFactory;
      }

      public static void Reset()
      {
         AppServices.Session.EndSession();
         AppServices.Reset();
      }
   }
}
