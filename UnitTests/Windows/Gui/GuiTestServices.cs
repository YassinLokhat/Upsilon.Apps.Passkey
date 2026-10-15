using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform.Fakes;
using Upsilon.Apps.Passkey.UnitTests.Windows.Gui.Fakes;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
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

      public static FakeClipboardManager Clipboard { get; private set; } = null!;

      public static FakeSessionService Session { get; private set; } = null!;

      public static FakeDialogService Dialogs { get; private set; } = null!;

      public static FakeNavigationService Navigation { get; private set; } = null!;

      public static void Install()
      {
         Monitor.Enter(_appServicesGate);
         _gateHeld = true;

         try
         {
            Clipboard = new FakeClipboardManager();
            Session = new FakeSessionService();
            Dialogs = new FakeDialogService();
            Navigation = new FakeNavigationService();

            Upsilon.Apps.Passkey.GUI.Presentation.Services.AppServices.Info = new GUI.WPF.Adapters.WpfAppInfo();
            AppServices.Clipboard = new ClipboardServiceAdapter(Clipboard);
            AppServices.Session = Session;
            AppServices.Dialogs = Dialogs;
            AppServices.Navigation = Navigation;
            AppServices.Cryptography = UnitTestsHelper.CryptographyCenter;
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

   /// <summary>Exposes the Core clipboard fake through the host <see cref="IClipboardService"/> port.</summary>
   internal sealed class ClipboardServiceAdapter(IClipboardManager inner) : IClipboardService
   {
      public event EventHandler<ClipboardSecretCopiedEventArgs>? SecretCopied;

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         inner.SetText(text, autoClearAfter);
         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            SecretCopied?.Invoke(this, new ClipboardSecretCopiedEventArgs(text));
         }
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
         => inner.RemoveAllOccurrenceAsync(removeList, cancellationToken);

      public void ClearIfStillOwned()
      {
      }
   }
}