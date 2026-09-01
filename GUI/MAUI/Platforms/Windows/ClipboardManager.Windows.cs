#if WINDOWS
using WinClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using WinDataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using WinDataPackageView = Windows.ApplicationModel.DataTransfer.DataPackageView;
using Windows.ApplicationModel.DataTransfer;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   internal sealed partial class ClipboardManager
   {
      private static partial void _setTextCore(string text)
      {
         WinDataPackage package = new();
         package.SetText(text);

         // Keep secrets out of clipboard history and cloud clipboard (parity with WPF
         // ExcludeClipboardContentFromMonitoring / CanIncludeInClipboardHistory / CanUploadToCloudClipboard).
         ClipboardContentOptions options = new()
         {
            IsAllowedInHistory = false,
            IsRoamable = false,
         };

         try
         {
            if (!WinClipboard.SetContentWithOptions(package, options))
            {
               WinClipboard.SetContent(package);
            }

            WinClipboard.Flush();
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or UnauthorizedAccessException
            or NotSupportedException
            or TimeoutException)
         {
            Log.Error(ex, "Failed to write to Windows clipboard");
            throw;
         }
      }

      private static partial async Task<int> _removeHistoryAsync(IEnumerable<string> removeList, CancellationToken cancellationToken)
      {
         ArgumentNullException.ThrowIfNull(removeList);

         HashSet<string> toRemove = removeList as HashSet<string> ?? [.. removeList];
         int cleaned = 0;

         try
         {
            ClipboardHistoryItemsResult historyResult = await WinClipboard
               .GetHistoryItemsAsync()
               .AsTask(cancellationToken)
               .ConfigureAwait(false);

            foreach (ClipboardHistoryItem item in historyResult.Items)
            {
               cancellationToken.ThrowIfCancellationRequested();

               WinDataPackageView content = item.Content;
               if (!content.Contains(StandardDataFormats.Text))
               {
                  continue;
               }

               string text = await content.GetTextAsync().AsTask(cancellationToken).ConfigureAwait(false);
               if (toRemove.Contains(text) && WinClipboard.DeleteItemFromHistory(item))
               {
                  cleaned++;
               }
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or InvalidOperationException
            or UnauthorizedAccessException
            or NotSupportedException
            or TimeoutException)
         {
            Log.Error(ex, "Failed to scrub Windows clipboard history");
         }

         return cleaned;
      }
   }
}
#endif
