using Upsilon.Apps.Passkey.Interfaces.Utils;
using Windows.ApplicationModel.DataTransfer;

namespace Upsilon.Apps.Passkey.GUI.OSSpecific
{
   public class ClipboardManager : IClipboardManager
   {
      public int RemoveAllOccurence(string[] removeList)
      {
         int cleanedPasswordCount = 0;

         IReadOnlyList<ClipboardHistoryItem> clipboardHistory = Clipboard.GetHistoryItemsAsync().AsTask().GetAwaiter().GetResult().Items;

         foreach (ClipboardHistoryItem? item in clipboardHistory)
         {
            DataPackageView content = item.Content;
            if (content.Contains(StandardDataFormats.Text))
            {
               string text = content.GetTextAsync().AsTask().GetAwaiter().GetResult();

               if (removeList.Any(x => x == text))
               {
                  _ = Clipboard.DeleteItemFromHistory(item);
                  cleanedPasswordCount++;
               }
            }
         }

         return cleanedPasswordCount;
      }
   }
}
