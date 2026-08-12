using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Test stub: clipboard I/O is OS-specific and out of scope for Core unit tests.
   /// </summary>
   public class ClipboardManager : IClipboardManager
   {
      public void SetText(string text, TimeSpan? autoClearAfter) => throw new NotImplementedException();
      public void SetText(string text, int autoClearAfter) => throw new NotImplementedException();

      public Task<int> RemoveAllOccurrenceAsync(string[] removeList, CancellationToken cancellationToken = default)
         => Task.FromResult(0);
   }
}
