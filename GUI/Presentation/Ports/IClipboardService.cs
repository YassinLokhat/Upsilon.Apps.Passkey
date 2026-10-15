using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Host clipboard façade. Extends Core's <see cref="IClipboardManager"/> with
   /// platform-specific clear-if-owned behaviour.
   /// </summary>
   internal interface IClipboardService : IClipboardManager
   {
      /// <summary>Clear the clipboard when it still holds the last secret we wrote.</summary>
      void ClearIfStillOwned();
   }
}
