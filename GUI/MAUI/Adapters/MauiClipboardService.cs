using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   /// <summary>
   /// Cross-platform clipboard. Windows host should prefer a WinRT implementation later;
   /// Android marks sensitive content where the API allows.
   /// </summary>
   internal sealed class MauiClipboardService : IClipboardService, IDisposable
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

         Clipboard.Default.SetTextAsync(text).GetAwaiter().GetResult();

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
                     // Replaced by a newer copy.
                  }
               }, token);
            }
         }
      }

      public void SetText(string text, int autoClearAfter)
         => SetText(text, autoClearAfter > 0 ? TimeSpan.FromSeconds(autoClearAfter) : null);

      public Task<int> RemoveAllOccurrenceAsync(IEnumerable<string> removeList, CancellationToken cancellationToken = default)
      {
         // No clipboard-history scrub API on Android; Windows WinRT scrub is host-specific.
         ArgumentNullException.ThrowIfNull(removeList);
         return Task.FromResult(0);
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
               string? current = Clipboard.Default.HasText
                  ? Clipboard.Default.GetTextAsync().GetAwaiter().GetResult()
                  : null;
               if (string.Equals(current, tracked, StringComparison.Ordinal))
               {
                  Clipboard.Default.SetTextAsync(string.Empty).GetAwaiter().GetResult();
               }
            }
            catch (Exception ex) when (ex is InvalidOperationException or TaskCanceledException)
            {
               // Best-effort clear; foreground-only on Android 10+.
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
