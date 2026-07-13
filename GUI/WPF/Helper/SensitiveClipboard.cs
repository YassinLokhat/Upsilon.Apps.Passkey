using System.Windows;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Wraps <see cref="Clipboard"/> writes to mark the content as sensitive so
   /// Windows 10/11 excludes it from Cloud Clipboard and the Win+V history,
   /// and schedules an automatic clear after a configurable delay.
   /// </summary>
   /// <remarks>
   /// The format names are documented by Microsoft: applications that opt in
   /// to "ExcludeClipboardContentFromMonitoring" or set
   /// "CanIncludeInClipboardHistory"/"CanUploadToCloudClipboard" to false ask
   /// the OS to keep the payload out of any history/sync surface.
   /// </remarks>
   internal static class SensitiveClipboard
   {
      private const string ExcludeFormat = "ExcludeClipboardContentFromMonitoring";
      private const string HistoryFormat = "CanIncludeInClipboardHistory";
      private const string CloudFormat = "CanUploadToCloudClipboard";

      private static readonly object _autoClearLock = new();
      private static DispatcherTimer? _autoClearTimer;
      private static string? _trackedContent;

      /// <summary>
      /// Copies <paramref name="text"/> to the clipboard while flagging it as
      /// confidential. If <paramref name="autoClearAfter"/> is greater than
      /// <see cref="TimeSpan.Zero"/>, the clipboard is cleared after that
      /// delay (unless it has been replaced by something else meanwhile).
      /// </summary>
      public static void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         DataObject data = new();
         data.SetText(text);
         data.SetData(ExcludeFormat, true);
         data.SetData(HistoryFormat, false);
         data.SetData(CloudFormat, false);

         try
         {
            Clipboard.SetDataObject(data, copy: true);
         }
         catch (Exception ex)
         {
            Log.Error(ex, "Failed to write to clipboard");
            return;
         }

         if (autoClearAfter is { } delay && delay > TimeSpan.Zero)
         {
            _scheduleAutoClear(text, delay);
         }
      }

      /// <summary>
      /// Clears the clipboard if (and only if) its current text content still
      /// matches the value previously written through <see cref="SetText"/>.
      /// </summary>
      public static void ClearIfStillOwned()
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
            catch (Exception ex)
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
