namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// OS-specific clipboard (auto-clear and history scrub).
   /// </summary>
   public interface IClipboardManager
   {
      /// <summary>
      /// Puts <paramref name="text"/> on the clipboard and, if
      /// <paramref name="autoClearAfter"/> is set, clears it later — but only if
      /// the clipboard still holds that same text.
      /// </summary>
      /// <param name="text">The text to add.</param>
      /// <param name="autoClearAfter">How long to keep the secret on the clipboard; <see langword="null"/> means no auto-clear.</param>
      void SetText(string text, TimeSpan? autoClearAfter = null);

      /// <summary>
      /// Same as <see cref="SetText(string, TimeSpan?)"/> with a delay in seconds
      /// (0 or negative means no auto-clear).
      /// </summary>
      /// <param name="text">The text to add.</param>
      /// <param name="autoClearAfter">Seconds to keep the secret on the clipboard.</param>
      void SetText(string text, int autoClearAfter);

      /// <summary>
      /// Remove any occurrence of elements in the given list from the clipboard history.
      /// WinRT clipboard history APIs are asynchronous; callers must not block a UI
      /// or timer thread waiting for this method.
      /// </summary>
      /// <param name="removeList">The list of elements to remove.</param>
      /// <param name="cancellationToken">Token used to cancel the history scan.</param>
      /// <returns>The number of item removed.</returns>
      Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default);
   }
}
