using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Host clipboard façade. Extends Core's <see cref="IClipboardManager"/> with
   /// platform-specific clear-if-owned behaviour. Implementations must mark
   /// copied values as sensitive where the platform allows it, and must be
   /// callable from any thread.
   /// </summary>
   internal interface IClipboardService : IClipboardManager
   {
      /// <summary>
      /// Raised after a value was copied with an auto-clear delay, so the
      /// session can remember it for the clipboard-history scrub. Handlers must
      /// never log the value.
      /// </summary>
      event EventHandler<ClipboardSecretCopiedEventArgs>? SecretCopied;

      /// <summary>Clear the clipboard when it still holds the last secret we wrote.</summary>
      void ClearIfStillOwned();
   }

   internal sealed class ClipboardSecretCopiedEventArgs(string text) : EventArgs
   {
      public string Text { get; } = text;
   }
}
