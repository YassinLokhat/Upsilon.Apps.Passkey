#if WINDOWS
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using WinClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using WinDataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using WinDataPackageView = Windows.ApplicationModel.DataTransfer.DataPackageView;
using WinClipboardContentOptions = Windows.ApplicationModel.DataTransfer.ClipboardContentOptions;
using WinClipboardHistoryItemsResult = Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResult;
using WinClipboardHistoryItem = Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem;
using WinStandardDataFormats = Windows.ApplicationModel.DataTransfer.StandardDataFormats;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Windows
{
   /// <summary>
   /// WinRT clipboard with history/roam disabled. Scrubs clipboard history when possible.
   /// </summary>
   internal sealed class WindowsClipboardService : IClipboardService, IDisposable
   {
      private readonly Lock _gate = new();
      private string? _tracked;
      private CancellationTokenSource? _clearCts;
      private bool _disposed;

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         WinDataPackage package = new();
         package.SetText(text);
         _ = WinClipboard.SetContentWithOptions(package, new WinClipboardContentOptions
         {
            IsAllowedInHistory = false,
            IsRoamable = false,
         });
         WinClipboard.Flush();

         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            lock (_gate)
            {
               _tracked = text;
               _clearCts?.Cancel();
               _clearCts = new CancellationTokenSource();
               CancellationToken token = _clearCts.Token;
               _ = Task.Run(async () =>
               {
                  try
                  {
                     await Task.Delay(delay, token).ConfigureAwait(false);
                     ClearIfStillOwned();
                  }
                  catch (OperationCanceledException)
                  {
                     // Replaced.
                  }
               }, token);
            }
         }
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public async Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(removeList);
         HashSet<string> toRemove = removeList as HashSet<string> ?? [.. removeList];
         int cleaned = 0;

         try
         {
            WinClipboardHistoryItemsResult history = await WinClipboard.GetHistoryItemsAsync()
               .AsTask(cancellationToken)
               .ConfigureAwait(false);

            foreach (WinClipboardHistoryItem item in history.Items)
            {
               cancellationToken.ThrowIfCancellationRequested();
               WinDataPackageView content = item.Content;
               if (!content.Contains(WinStandardDataFormats.Text))
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
         catch (Exception ex) when (ex is InvalidOperationException
            or UnauthorizedAccessException
            or NotImplementedException
            or System.Runtime.InteropServices.COMException)
         {
            // History APIs may be unavailable; best-effort.
         }

         return cleaned;
      }

      public void ClearIfStillOwned()
      {
         lock (_gate)
         {
            string? tracked = _tracked;
            _tracked = null;
            if (tracked is null)
            {
               return;
            }

            try
            {
               WinDataPackageView view = WinClipboard.GetContent();
               if (!view.Contains(WinStandardDataFormats.Text))
               {
                  return;
               }

               string? current = view.GetTextAsync().AsTask().GetAwaiter().GetResult();
               if (string.Equals(current, tracked, StringComparison.Ordinal))
               {
                  WinClipboard.Clear();
               }
            }
            catch (Exception ex) when (ex is InvalidOperationException
               or UnauthorizedAccessException
               or TaskCanceledException
               or System.Runtime.InteropServices.COMException)
            {
               // Best-effort.
            }
         }
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         lock (_gate)
         {
            _clearCts?.Cancel();
            _clearCts?.Dispose();
            _clearCts = null;
            _tracked = null;
         }
      }
   }
}
#endif
