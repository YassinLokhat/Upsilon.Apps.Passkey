using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>
   /// Login-screen inactivity countdown from <see cref="AppSettings.LoginIdleTimeoutSeconds"/>
   /// (<c>0</c> disables it). Ticks once per second and raises <see cref="Elapsed"/>
   /// when it reaches zero, so the host can clear the typed credentials and any
   /// half-open session. Without a timer factory it stays off. Must be driven from
   /// the UI thread.
   /// </summary>
   internal sealed class LoginIdleTimer : IDisposable
   {
      private readonly IUiTimer? _timer;

      public LoginIdleTimer(IUiTimerFactory? timers)
      {
         _timer = timers?.Create();
         if (_timer is not null)
         {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _onTick;
         }
      }

      /// <summary>Seconds left before the reset; <c>0</c> while the countdown is off.</summary>
      public int SecondsRemaining { get; private set; }

      /// <summary>Raised whenever <see cref="SecondsRemaining"/> changes.</summary>
      public event EventHandler? Changed;

      public event EventHandler? Elapsed;

      /// <summary>(Re)starts the countdown from the configured timeout.</summary>
      public void Restart()
      {
         int timeout = Math.Max(0, AppServices.Settings?.Current.LoginIdleTimeoutSeconds ?? AppSettings.DEFAULT_LOGIN_IDLE_TIMEOUT_SECONDS);
         if (_timer is null || timeout == 0)
         {
            Stop();
            return;
         }

         _timer.StopTimer();
         _setRemaining(timeout);
         _timer.Start();
      }

      public void Stop()
      {
         _timer?.StopTimer();
         _setRemaining(0);
      }

      /// <summary><paramref name="title"/>, followed by the countdown while it runs.</summary>
      public string AppendCountdown(string title)
         => SecondsRemaining > 0
            ? title + Strings.Format(nameof(Strings.Title_IdleResetCredentialTimeout), SecondsRemaining)
            : title;

      public void Dispose()
      {
         if (_timer is not null)
         {
            _timer.Tick -= _onTick;
            _timer.Dispose();
         }

         Changed = null;
         Elapsed = null;
      }

      private void _onTick(object? sender, EventArgs e)
      {
         // A tick queued before Stop may still arrive.
         if (SecondsRemaining == 0)
         {
            return;
         }

         if (SecondsRemaining > 1)
         {
            _setRemaining(SecondsRemaining - 1);
            return;
         }

         Stop();
         Elapsed?.Invoke(this, EventArgs.Empty);
      }

      private void _setRemaining(int seconds)
      {
         if (SecondsRemaining == seconds)
         {
            return;
         }

         SecondsRemaining = seconds;
         Changed?.Invoke(this, EventArgs.Empty);
      }
   }
}
