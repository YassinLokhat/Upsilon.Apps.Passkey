using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiUiTimerFactory : IUiTimerFactory
   {
      public IUiTimer Create() => new MauiUiTimer();

      private sealed class MauiUiTimer : IUiTimer
      {
         private CancellationTokenSource? _cts;
         private bool _running;

         public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

         public event EventHandler? Tick;

         public void Start()
         {
            if (_running)
            {
               return;
            }

            _running = true;
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            _ = Task.Run(async () =>
            {
               try
               {
                  while (!token.IsCancellationRequested)
                  {
                     await Task.Delay(Interval, token).ConfigureAwait(false);
                     if (token.IsCancellationRequested)
                     {
                        break;
                     }

                     MainThread.BeginInvokeOnMainThread(() => Tick?.Invoke(this, EventArgs.Empty));
                  }
               }
               catch (OperationCanceledException)
               {
                  // Stopped.
               }
            }, token);
         }

         public void StopTimer()
         {
            _running = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
         }

         public void Dispose() => StopTimer();
      }
   }
}
