using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   /// <summary>
   /// Cross-platform clipboard with auto-clear. Windows may scrub clipboard history
   /// via the platform partial; Android only clears the current clipboard text.
   /// </summary>
   internal sealed partial class ClipboardManager : IClipboardManager
   {
      private static readonly object _autoClearLock = new();
      private static CancellationTokenSource? _autoClearCts;
      private static string? _trackedContent;

      internal static int AutoClearAfter
         => AppServices.Session.User?.Settings.CleaningClipboardTimeout ?? 0;

      public void SetText(string text, TimeSpan? autoClearAfter = null)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         try
         {
            _setPlatformText(text);
         }
         catch (Exception ex)
            when (ex is ArgumentNullException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or TimeoutException
            or TaskCanceledException
            or OperationCanceledException)
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
         TimeSpan? autoClear = autoClearAfter > 0
            ? TimeSpan.FromSeconds(autoClearAfter)
            : null;
         SetText(text, autoClear);
      }

      public Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
         => _removeHistoryAsync(removeList, cancellationToken);

      internal static void ClearIfStillOwned()
      {
         lock (_autoClearLock)
         {
            string? tracked = _trackedContent;
            _trackedContent = null;
            _autoClearCts?.Cancel();
            _autoClearCts = null;

            if (tracked is null)
            {
               return;
            }

            _ = _clearIfMatchesAsync(tracked);
         }
      }

      private static void _scheduleAutoClear(string text, TimeSpan delay)
      {
         lock (_autoClearLock)
         {
            _trackedContent = text;
            _autoClearCts?.Cancel();
            CancellationTokenSource cts = new();
            _autoClearCts = cts;

            _ = Task.Run(async () =>
            {
               try
               {
                  await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                  ClearIfStillOwned();
               }
               catch (OperationCanceledException)
               {
                  // superseded or session ended
               }
            }, CancellationToken.None);
         }
      }

      private static async Task _clearIfMatchesAsync(string tracked)
      {
         try
         {
            string current = await Clipboard.Default.GetTextAsync().ConfigureAwait(false) ?? string.Empty;
            if (current == tracked)
            {
               await Clipboard.Default.SetTextAsync(string.Empty).ConfigureAwait(false);
            }
         }
         catch (Exception ex)
            when (ex is ArgumentNullException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or TimeoutException
            or TaskCanceledException
            or OperationCanceledException)
         {
            Log.Error(ex, "Failed to clear sensitive clipboard content");
         }
      }

      private static void _setPlatformText(string text)
         => _setTextCore(text);

      /// <summary>Platform write (Windows excludes history/cloud; Android uses MAUI clipboard).</summary>
      private static partial void _setTextCore(string text);

      private static partial Task<int> _removeHistoryAsync(IEnumerable<string> removeList, CancellationToken cancellationToken);
   }
}
