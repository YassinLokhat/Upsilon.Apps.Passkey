using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Windows.ApplicationModel.DataTransfer;
using Clipboard = System.Windows.Clipboard;

namespace Upsilon.Apps.Passkey.GUI.WPF.OSSpecific
{
   internal sealed class ClipboardManager : IClipboardManager
   {
      #region IAccount interface explicit Internal

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         DataObject data = new();
         data.SetText(text);
         data.SetData(EXCLUDE_FORMAT, true);
         data.SetData(HISTORY_FORMAT, false);
         data.SetData(CLOUD_FORMAT, false);

         try
         {
            Clipboard.SetDataObject(data, copy: true);
         }
#pragma warning disable CA1031 // Last-resort barrier: a clipboard failure must never crash the caller
         catch (Exception ex)
#pragma warning restore CA1031
         {
            Log.Error(ex, "Failed to write to clipboard");
            return;
         }

         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            _scheduleAutoClear(text, delay);
         }
      }

      public void SetText(string text, int autoClearAfter)
      {
         TimeSpan? autoClear = null;

         if (autoClearAfter > 0)
         {
            autoClear = TimeSpan.FromSeconds(autoClearAfter);
         }

         SetText(text, autoClear);
      }

      public int RemoveAllOccurrence(string[] removeList)
      {
         int cleanedPasswordCount = 0;

         IReadOnlyList<ClipboardHistoryItem> clipboardHistory = Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync().AsTask().GetAwaiter().GetResult().Items;

         foreach (ClipboardHistoryItem? item in clipboardHistory)
         {
            DataPackageView content = item.Content;
            if (content.Contains(StandardDataFormats.Text))
            {
               string text = content.GetTextAsync().AsTask().GetAwaiter().GetResult();

               if (removeList.Any(x => x == text))
               {
                  _ = Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(item);
                  cleanedPasswordCount++;
               }
            }
         }

         return cleanedPasswordCount;
      }

      #endregion

      private const string EXCLUDE_FORMAT = "ExcludeClipboardContentFromMonitoring";
      private const string HISTORY_FORMAT = "CanIncludeInClipboardHistory";
      private const string CLOUD_FORMAT = "CanUploadToCloudClipboard";

      private static readonly object _autoClearLock = new();
      private static DispatcherTimer? _autoClearTimer;
      private static string? _trackedContent;

      internal static int AutoClearAfter
         => AppServices.Session.User?.Settings.CleaningClipboardTimeout ?? 0;

      /// <summary>
      /// Clears the clipboard if (and only if) its current text content still
      /// matches the value previously written through <see cref="SetText"/>.
      /// </summary>
      internal static void ClearIfStillOwned()
      {
         lock (_autoClearLock)
         {
            string? tracked = _trackedContent;
            _trackedContent = null;
            _autoClearTimer?.Stop();
            _autoClearTimer = null;

            if (tracked is null) return;

            try
            {
               if (Clipboard.ContainsText() && Clipboard.GetText() == tracked)
               {
                  Clipboard.Clear();
               }
            }
#pragma warning disable CA1031 // Last-resort barrier: a clipboard failure must never crash the caller
            catch (Exception ex)
#pragma warning restore CA1031
            {
               Log.Error(ex, "Failed to clear sensitive clipboard content");
            }
         }
      }

      private static void _scheduleAutoClear(string text, TimeSpan delay)
      {
         lock (_autoClearLock)
         {
            _trackedContent = text;
            _autoClearTimer?.Stop();

            _autoClearTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
               Interval = delay,
            };
            _autoClearTimer.Tick += (_, _) => ClearIfStillOwned();
            _autoClearTimer.Start();
         }
      }

   }
}
