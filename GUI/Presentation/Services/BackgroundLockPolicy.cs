using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Ends the open session when the app comes back after staying in the background
   /// for at least <see cref="Models.AppSettings.BackgroundLockSeconds"/>. The check runs
   /// on resume, after the background flush, because closing from the background would
   /// race the host's flush and may never complete if the process is suspended.
   /// </summary>
   internal sealed class BackgroundLockPolicy : IDisposable
   {
      private readonly IAppLifecycle _lifecycle;
      private readonly Func<DateTime> _utcNow;
      private DateTime? _backgroundSinceUtc;

      public BackgroundLockPolicy(IAppLifecycle lifecycle, Func<DateTime>? utcNow = null)
      {
         _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
         _utcNow = utcNow ?? (static () => DateTime.UtcNow);
         _lifecycle.EnteredBackground += _onEnteredBackground;
         _lifecycle.Resumed += _onResumed;
      }

      public void Dispose()
      {
         _lifecycle.EnteredBackground -= _onEnteredBackground;
         _lifecycle.Resumed -= _onResumed;
      }

      private void _onEnteredBackground(object? sender, EventArgs e)
         => _backgroundSinceUtc ??= _utcNow();

      private void _onResumed(object? sender, EventArgs e)
      {
         DateTime? since = _backgroundSinceUtc;
         _backgroundSinceUtc = null;
         if (since is not { } backgroundSinceUtc || AppServices.Session.Database is null)
         {
            return;
         }

         int lockAfter = Math.Max(0, AppServices.Settings?.Current.BackgroundLockSeconds ?? Models.AppSettings.DEFAULT_BACKGROUND_LOCK_SECONDS);
         if (_utcNow() - backgroundSinceUtc < TimeSpan.FromSeconds(lockAfter))
         {
            return;
         }

         // Closing flushes pending edits and queues the final write-back, so the
         // vault is hidden at once instead of after a possibly slow upload.
         AppServices.Log.Info("Session locked after staying in the background.");
         AppServices.Session.EndSession();
      }
   }
}
