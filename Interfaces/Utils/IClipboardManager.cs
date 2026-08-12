namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Represent a OS specific Clipboard manager.
   /// </summary>
   public interface IClipboardManager
   {
      /// <summary>
      /// Add the given text to the clipboard then clear if after a certain time if set.
      /// </summary>
      /// <param name="text">The text to add.</param>
      /// <param name="autoClearAfter">The duration to keep the password in the clipboard.</param>
      void SetText(string text, TimeSpan? autoClearAfter = null);

      /// <summary>
      /// Add the given text to the clipboard then clear if after a certain time if set.
      /// </summary>
      /// <param name="text">The text to add.</param>
      /// <param name="autoClearAfter">The duration to keep the password in the clipboard.</param>
      void SetText(string text, int autoClearAfter);

      /// <summary>
      /// Remove any occurrence of elements in the given list from the clipboard history.
      /// WinRT clipboard history APIs are asynchronous; callers must not block a UI
      /// or timer thread waiting for this method.
      /// </summary>
      /// <param name="removeList">The list of elements to remove.</param>
      /// <param name="cancellationToken">Token used to cancel the history scan.</param>
      /// <returns>The number of item removed.</returns>
      Task<int> RemoveAllOccurrenceAsync(string[] removeList, CancellationToken cancellationToken = default);
   }
}
