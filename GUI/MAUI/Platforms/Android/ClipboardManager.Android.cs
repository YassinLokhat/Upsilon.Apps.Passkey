#if ANDROID
namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   internal sealed partial class ClipboardManager
   {
      private static partial void _setTextCore(string text)
         => Clipboard.Default.SetTextAsync(text).GetAwaiter().GetResult();

      private static partial Task<int> _removeHistoryAsync(IEnumerable<string> removeList, CancellationToken cancellationToken)
      {
         // Android has no clipboard-history API comparable to WinRT.
         _ = removeList;
         _ = cancellationToken;
         return Task.FromResult(0);
      }
   }
}
#endif
