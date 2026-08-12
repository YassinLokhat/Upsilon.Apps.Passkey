namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Coalesces rapid dirty marks into a single disk write after a short delay.
   /// <see cref="Flush"/> forces an immediate write of any pending work; call it
   /// before Close/Dispose so nothing is left sitting in the timer.
   /// </summary>
   internal sealed class DeferredPersistence : IDisposable
   {
      // Long enough to absorb a burst of field edits / activity events into one
      // ZIP rewrite, short enough that a crash shortly after editing still leaves
      // an autosave/activity file on disk.
      private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(500);

      private readonly Lock _gate = new();
      private readonly Action _persist;
      private readonly TimeSpan _delay;
      private Timer? _timer;
      private bool _dirty;
      private bool _disposed;

      internal DeferredPersistence(Action persist, TimeSpan? delay = null)
      {
         ArgumentNullException.ThrowIfNull(persist);
         _persist = persist;
         _delay = delay ?? DefaultDelay;
      }

      /// <summary>
      /// Marks pending work and (re)arms the debounce timer. Repeated calls within
      /// <see cref="DefaultDelay"/> collapse into a single persistence pass.
      /// </summary>
      internal void Schedule()
      {
         lock (_gate)
         {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _dirty = true;
            _timer ??= new Timer(_onTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _ = _timer.Change(_delay, Timeout.InfiniteTimeSpan);
         }
      }

      /// <summary>
      /// Writes immediately if anything is pending, and cancels the debounce timer.
      /// Safe to call when nothing is dirty. If <c>_persist</c> throws, dirty is
      /// re-armed so a later Flush (or timer) can retry.
      /// </summary>
      internal void Flush()
      {
         bool shouldPersist;

         lock (_gate)
         {
            if (_disposed)
            {
               return;
            }

            _cancelTimer_NoLock();
            shouldPersist = _dirty;
            _dirty = false;
         }

         if (!shouldPersist)
         {
            return;
         }

         try
         {
            _persist();
         }
         catch
         {
            lock (_gate)
            {
               if (!_disposed)
               {
                  _dirty = true;
               }
            }

            throw;
         }
      }

      /// <summary>
      /// Drops any pending write without touching the disk. Used when the pending
      /// state is discarded (e.g. AutoSave cleared after a successful Save).
      /// </summary>
      internal void Cancel()
      {
         lock (_gate)
         {
            if (_disposed)
            {
               return;
            }

            _dirty = false;
            _cancelTimer_NoLock();
         }
      }

      public void Dispose()
      {
         lock (_gate)
         {
            if (_disposed)
            {
               return;
            }

            _disposed = true;
            _dirty = false;
            _cancelTimer_NoLock();
         }
      }

      private void _onTimer(object? state)
      {
         try
         {
            Flush();
         }
#pragma warning disable CA1031 // Last-resort barrier: a background flush must never tear down the process
         catch (Exception ex)
#pragma warning restore CA1031
         {
            // Dirty is already re-armed by Flush on failure; this catch only
            // keeps the ThreadPool callback from throwing.
            System.Diagnostics.Trace.TraceWarning($"Deferred persistence flush failed: {ex}");
         }
      }

      private void _cancelTimer_NoLock()
      {
         if (_timer is null)
         {
            return;
         }

         _ = _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
         _timer.Dispose();
         _timer = null;
      }
   }
}
