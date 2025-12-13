namespace Upsilon.Apps.Passkey.Interfaces
{
   /// <summary>
   /// Represent a OS specific Clipboard manager.
   /// </summary>
   public interface IClipboardManager
   {
      /// <summary>
      /// Remove any occurence of elements in a the given list from the clipboard history.
      /// </summary>
      /// <param name="removeList">The list of eleemnts to remove.</param>
      /// <returns>The number of item removed.</returns>
      int RemoveAllOccurence(string[] removeList);
   }
}
