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
      void SetText(string text, TimeSpan? autoClearAfter);

      /// <summary>
      /// Add the given text to the clipboard then clear if after a certain time if set.
      /// </summary>
      /// <param name="text">The text to add.</param>
      /// <param name="autoClearAfter">The duration to keep the password in the clipboard.</param>
      void SetText(string text, int autoClearAfter);

      /// <summary>
      /// Remove any occurrence of elements in the given list from the clipboard history.
      /// </summary>
      /// <param name="removeList">The list of elements to remove.</param>
      /// <returns>The number of item removed.</returns>
      int RemoveAllOccurrence(string[] removeList);
   }
}
