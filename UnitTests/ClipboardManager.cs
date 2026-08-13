using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Test stub: clipboard I/O is OS-specific. Records SetText calls so GUI
   /// ViewModel tests can assert without touching the real clipboard.
   /// </summary>
   public class ClipboardManager : IClipboardManager
   {
      public string? LastText { get; private set; }

      public TimeSpan? LastAutoClearAfter { get; private set; }

      public IReadOnlyList<string> Texts => _texts;

      private readonly List<string> _texts = [];

      public void SetText(string text, TimeSpan? autoClearAfter)
      {
         LastText = text;
         LastAutoClearAfter = autoClearAfter;
         _texts.Add(text);
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public Task<int> RemoveAllOccurrenceAsync(string[] removeList, CancellationToken cancellationToken = default)
         => Task.FromResult(0);

      public void Clear()
      {
         LastText = null;
         LastAutoClearAfter = null;
         _texts.Clear();
      }
   }
}
