namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Represent a OS specific Clipboard manager.
   /// </summary>
   public interface IClipboardManager
   {
      /// <summary>
      /// Remove any occurrence of elements in the given list from the clipboard history.
      /// </summary>
      /// <param name="removeList">The list of elements to remove.</param>
      /// <returns>The number of item removed.</returns>
      int RemoveAllOccurrence(string[] removeList);
   }
}
