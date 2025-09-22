using Windows.ApplicationModel.DataTransfer;

namespace Upsilon.Apps.PassKey.Core.Public.Utils
{
   public static class ClipboardManager
   {
      public static void RemoveAllOccurence(string[] removeList)
      {
         var clipboardHistory = Clipboard.GetHistoryItemsAsync().AsTask().GetAwaiter().GetResult().Items;

         foreach (var item in clipboardHistory)
         {
            var content = item.Content;
            if (content.Contains(StandardDataFormats.Text))
            {
               string text = content.GetTextAsync().AsTask().GetAwaiter().GetResult();

               if (removeList.Any(x => x == text))
               {
                  Clipboard.DeleteItemFromHistory(item);
               }
            }
         }
      }
   }
}
