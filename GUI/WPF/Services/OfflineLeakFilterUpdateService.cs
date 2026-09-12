using System.IO;
using System.Net.Http;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Utils;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// Serializes manual and automatic offline leak-filter builds/updates so only
   /// one run touches the <c>.pkbf</c> at a time. Detaches the memory-mapped
   /// filter for the duration of the write, then reloads it.
   /// </summary>
   internal sealed class OfflineLeakFilterUpdateService
   {
      private readonly Lock _gate = new();
      private CancellationTokenSource? _cts;
      private int _busy;

      public bool IsBusy => Volatile.Read(ref _busy) != 0;

      /// <summary>
      /// True once <see cref="Cancel"/> has been observed for the in-flight run
      /// (or the linked token was cancelled), until the run finishes.
      /// </summary>
      public bool IsCancellationRequested
      {
         get
         {
            lock (_gate)
            {
               return _cts?.IsCancellationRequested == true;
            }
         }
      }

      /// <summary>
      /// Most recent progress snapshot from the in-flight (or last) run.
      /// </summary>
      public HibpBloomBuildProgress? LatestProgress
      {
         get
         {
            lock (_gate)
            {
               return field;
            }
         }

         private set;
      }

      /// <summary>
      /// Result of the last completed run, if it finished without throwing.
      /// </summary>
      public HibpBloomBuildResult? LatestResult
      {
         get
         {
            lock (_gate)
            {
               return field;
            }
         }

         private set;
      }

      /// <summary>
      /// True when the last run ended because of cancellation.
      /// </summary>
      public bool WasCancelled
      {
         get
         {
            lock (_gate)
            {
               return field;
            }
         }

         private set;
      }

      public event EventHandler? BusyChanged;

      /// <summary>
      /// Raised whenever <see cref="LatestProgress"/> is updated (any thread).
      /// </summary>
      public event EventHandler? ProgressChanged;

      /// <summary>
      /// When <see langword="true"/>, <see cref="App.OnExit"/> must wait with
      /// <see cref="WaitUntilIdle"/> instead of <see cref="WaitForIdle"/> so the
      /// consented background refresh is not cancelled. The main window sets this
      /// after <c>EndSession</c> + Hide when the user chooses Yes on the exit
      /// prompt, then defers <see cref="Application.Shutdown"/> until idle.
      /// </summary>
      public bool ContinueThroughExit { get; set; }

      /// <summary>
      /// When <see langword="true"/>, <see cref="MainWindow"/> must not show the
      /// exit prompt again (the vault window already handled Yes/No).
      /// </summary>
      public bool SkipClosePrompt { get; set; }

      /// <summary>
      /// Cancels the in-flight run, if any. Safe to call when idle.
      /// </summary>
      public void Cancel()
      {
         // Snapshot under the gate, then Cancel outside it: CTS.Cancel can run
         // callbacks synchronously, and those (or a Dispatcher marshal they
         // trigger) may need _gate. System.Threading.Lock is non-recursive, so
         // holding it across Cancel deadlocks after the object→Lock switch.
         CancellationTokenSource? cts;
         lock (_gate)
         {
            cts = _cts;
         }

         try
         {
            cts?.Cancel();
         }
         catch (ObjectDisposedException)
         {
            // Race with RunAsync's using-dispose after _cts was cleared.
         }
      }

      /// <summary>
      /// Blocks until any in-flight run finishes or <paramref name="timeout"/>
      /// elapses. Does <strong>not</strong> cancel the run.
      /// </summary>
      /// <param name="timeout">Maximum time to wait; negative values are treated as zero.</param>
      /// <returns>
      /// <see langword="true"/> if the service is idle (including already idle);
      /// <see langword="false"/> if it is still busy when the timeout elapses.
      /// </returns>
      public bool WaitUntilIdle(TimeSpan timeout)
      {
         if (!IsBusy)
         {
            return true;
         }

         TimeSpan remaining = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;
         try
         {
            return SpinWait.SpinUntil(() => !IsBusy, remaining);
         }
         catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
         {
            return !IsBusy;
         }
      }

      /// <summary>
      /// Cancels any in-flight run and blocks until it finishes or
      /// <paramref name="timeout"/> elapses. Used on process exit when
      /// <see cref="ContinueThroughExit"/> is <see langword="false"/>.
      /// </summary>
      /// <param name="timeout">Maximum time to wait after cancellation.</param>
      /// <returns>
      /// <see langword="true"/> if idle within the timeout; otherwise
      /// <see langword="false"/>.
      /// </returns>
      public bool WaitForIdle(TimeSpan timeout)
      {
         Cancel();
         return WaitUntilIdle(timeout);
      }

      /// <summary>
      /// Starts a background refresh when offline use, auto-update, an existing
      /// <c>.pkbf</c>, and its <c>.ranges</c> sidecar are all set. Never builds
      /// from scratch, and never re-downloads the corpus when the sidecar is gone.
      /// </summary>
      public void TryStartAutoUpdate()
      {
         LeakFilterConfig config = AppInfo.AppSettings.LeakFilterConfig;

         if (!config.Enabled
            || config.AutoUpdateFrequency == 0
            || !File.Exists(config.FilterPath))
         {
            return;
         }

         FileInfo info = new(config.FilterPath);
         DateTime lastUpdateTime = AppInfo.AppSettings.LeakFilterConfig.TryGetBuiltUtc(out DateTime builtUtc)
            ? builtUtc
            : info.LastWriteTimeUtc;

         if (DateTime.Now.Date <= lastUpdateTime.AddDays(config.AutoUpdateFrequency).Date)
         {
            return;
         }

         // No ETags ⇒ no cheap refresh. Starting Update here used to recreate an
         // empty sidecar and pull every range, which kept the process alive after
         // the UI closed.
         if (!File.Exists(HibpBloomBuilder.GetRangeStatePath(config.FilterPath)))
         {
            Log.Info(
               "Offline leak filter: auto-update skipped (range sidecar missing); "
               + "keeping the existing .pkbf. Use Rebuild from settings to restore incremental updates.");
            return;
         }

         if (IsBusy)
         {
            return;
         }

         Log.Info("Offline leak filter: starting background auto-update.");

         _ = Task.Run(async () =>
         {
            try
            {
               HibpBloomBuildResult? result = await RunAsync(
                  HibpBloomBuildMode.Update,
                  progress: null,
                  cancellationToken: CancellationToken.None).ConfigureAwait(false);

               if (result is null)
               {
                  return;
               }

               if (result.Value.Skipped)
               {
                  Log.Info("Offline leak filter: auto-update skipped (already up to date or nothing to do).");
               }
               else
               {
                  Log.Info(
                     $"Offline leak filter: auto-update complete "
                     + $"({result.Value.ChangedPrefixes} ranges refreshed, "
                     + $"{result.Value.UnchangedPrefixes} unchanged, "
                     + $"{result.Value.DownloadedBytes} bytes downloaded).");
               }
            }
            catch (OperationCanceledException)
            {
               Log.Info("Offline leak filter: auto-update cancelled.");
            }
            catch (Exception ex)
               when (ex is ArgumentException
               or HttpRequestException
               or IOException
               or UnauthorizedAccessException)
            {
               Log.Error(ex, "Offline leak filter: auto-update failed");
            }
         });
      }

      /// <summary>
      /// Runs a build or update. Returns <see langword="null"/> when another run
      /// is already in progress.
      /// </summary>
      public async Task<HibpBloomBuildResult?> RunAsync(
         HibpBloomBuildMode mode,
         IProgress<HibpBloomBuildProgress>? progress = null,
         CancellationToken cancellationToken = default)
      {
         if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
         {
            return null;
         }

         lock (_gate)
         {
            LatestProgress = null;
            LatestResult = null;
            WasCancelled = false;
         }

         BusyChanged?.Invoke(this, EventArgs.Empty);

         using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
         lock (_gate)
         {
            _cts = linkedCts;
         }

         // Fan progress out to any AppSettings window that may open mid-run, and
         // to the optional caller-provided reporter (manual build UI).
         IProgress<HibpBloomBuildProgress> combined = new Progress<HibpBloomBuildProgress>(p =>
         {
            lock (_gate)
            {
               LatestProgress = p;
            }

            ProgressChanged?.Invoke(this, EventArgs.Empty);
            progress?.Report(p);
         });

         // An attached filter maps the .pkbf read-only but shares it read-only too,
         // which denies the read-write handle an in-place refresh needs.
         if (AppServices.PasswordFactory is PasswordFactory detaching)
         {
            detaching.AttachLocalFilter(null);
         }

         bool cancelled = false;

         try
         {
            string filterPath = AppInfo.AppSettings.LeakFilterConfig.FilterPath;
            HibpBloomBuildResult result = await HibpBloomBuilder.RunAsync(
               filterPath,
               mode,
               progress: combined,
               cancellationToken: linkedCts.Token).ConfigureAwait(false);

            lock (_gate)
            {
               LatestResult = result;
            }

            return result;
         }
         catch (OperationCanceledException)
         {
            cancelled = true;
            throw;
         }
         finally
         {
            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.ReloadLocalFilter(AppInfo.AppSettings.LeakFilterConfig);
            }

            lock (_gate)
            {
               if (ReferenceEquals(_cts, linkedCts))
               {
                  _cts = null;
               }

               WasCancelled = cancelled || linkedCts.IsCancellationRequested;
            }

            _ = Interlocked.Exchange(ref _busy, 0);
            BusyChanged?.Invoke(this, EventArgs.Empty);
         }
      }
   }
}
