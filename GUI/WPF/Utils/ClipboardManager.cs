using System.Runtime.InteropServices;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Windows.ApplicationModel.DataTransfer;
using Clipboard = System.Windows.Clipboard;

namespace Upsilon.Apps.Passkey.GUI.WPF.Utils
{
   /// <summary>
   /// Windows clipboard: writes with history/cloud/monitor exclusion formats,
   /// auto-clears only while the text is still ours, and scrubs WinRT clipboard
   /// history asynchronously. Clipboard access is marshalled to the UI (STA)
   /// thread, so every member is callable from any thread.
   /// </summary>
   internal sealed class ClipboardManager : IClipboardService, IDisposable
   {
      // Windows clipboard data formats that keep a secret out of history, cloud
      // clipboard, and clipboard-monitoring apps (undocumented but widely used).
      private const string EXCLUDE_FORMAT = "ExcludeClipboardContentFromMonitoring";
      private const string HISTORY_FORMAT = "CanIncludeInClipboardHistory";
      private const string CLOUD_FORMAT = "CanUploadToCloudClipboard";

      private readonly Lock _autoClearLock = new();
      private Timer? _autoClearTimer;
      private string? _trackedContent;
      private bool _disposed;

      public event EventHandler<ClipboardSecretCopiedEventArgs>? SecretCopied;

      internal static int AutoClearAfter
         => AppServices.Session.User?.Settings.CleaningClipboardTimeout ?? 0;

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         bool written = false;
         UiThread.Send(() => written = _write(text));
         if (!written)
         {
            return;
         }

         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            // Session scrub tracks only values we auto-clear (passwords /
            // identifiers), not ad-hoc copies without a timeout.
            SecretCopied?.Invoke(this, new ClipboardSecretCopiedEventArgs(text));
            _scheduleAutoClear(text, delay);
         }
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public async Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(removeList);

         HashSet<string> toRemove = removeList as HashSet<string> ?? [.. removeList];
         int cleanedPasswordCount = 0;

         ClipboardHistoryItemsResult historyResult = await Windows.ApplicationModel.DataTransfer.Clipboard
            .GetHistoryItemsAsync()
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

         foreach (ClipboardHistoryItem item in historyResult.Items)
         {
            cancellationToken.ThrowIfCancellationRequested();

            DataPackageView content = item.Content;
            if (!content.Contains(StandardDataFormats.Text))
            {
               continue;
            }

            string text = await content.GetTextAsync().AsTask(cancellationToken).ConfigureAwait(false);

            if (toRemove.Contains(text)
                && Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(item))
            {
               cleanedPasswordCount++;
            }
         }

         return cleanedPasswordCount;
      }

      /// <summary>
      /// Clears the clipboard if (and only if) its current text content still
      /// matches the value previously written through <see cref="SetText(string, TimeSpan?)"/>.
      /// </summary>
      public void ClearIfStillOwned()
      {
         string? tracked;
         lock (_autoClearLock)
         {
            tracked = _trackedContent;
            _trackedContent = null;
            _autoClearTimer?.Dispose();
            _autoClearTimer = null;
         }

         if (tracked is not null)
         {
            UiThread.Send(() => _clearIfMatches(tracked));
         }
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;
         lock (_autoClearLock)
         {
            _autoClearTimer?.Dispose();
            _autoClearTimer = null;
            _trackedContent = null;
         }

         SecretCopied = null;
      }

      private static bool _write(string text)
      {
         DataObject data = new();
         data.SetText(text);
         data.SetData(EXCLUDE_FORMAT, true);
         data.SetData(HISTORY_FORMAT, false);
         data.SetData(CLOUD_FORMAT, false);

         try
         {
            Clipboard.SetDataObject(data, copy: true);
            return true;
         }
         catch (Exception ex)
            when (ex is ArgumentNullException
            or ExternalException)
         {
            Log.Error(ex, "Failed to write to clipboard");
            return false;
         }
      }

      private static void _clearIfMatches(string tracked)
      {
         try
         {
            if (Clipboard.ContainsText() && Clipboard.GetText() == tracked)
            {
               Clipboard.Clear();
            }
         }
         catch (Exception ex)
            when (ex is ArgumentNullException
            or ExternalException
            or ThreadStateException)
         {
            Log.Error(ex, "Failed to clear sensitive clipboard content");
         }
      }

      private void _scheduleAutoClear(string text, TimeSpan delay)
      {
         lock (_autoClearLock)
         {
            _trackedContent = text;
            _autoClearTimer?.Dispose();
            _autoClearTimer = new Timer(_ => ClearIfStillOwned(), null, delay, Timeout.InfiniteTimeSpan);
         }
      }
   }
}
