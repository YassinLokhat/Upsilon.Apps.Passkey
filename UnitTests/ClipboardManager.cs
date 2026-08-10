using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Windows.ApplicationModel.DataTransfer;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class ClipboardManager : IClipboardManager
   {
      public void SetText(string text, TimeSpan? autoClearAfter) => throw new NotImplementedException();
      public void SetText(string text, int autoClearAfter) => throw new NotImplementedException();

      public int RemoveAllOccurrence(string[] removeList)
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
