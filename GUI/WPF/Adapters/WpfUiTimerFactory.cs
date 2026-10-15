using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   internal sealed class WpfUiTimerFactory : IUiTimerFactory
   {
      public IUiTimer Create() => new WpfUiTimer();

      private sealed class WpfUiTimer : IUiTimer
      {
         private readonly DispatcherTimer _timer = new(DispatcherPriority.Background);

         public TimeSpan Interval
         {
            get => _timer.Interval;
            set => _timer.Interval = value;
         }

         public event EventHandler? Tick
         {
            add => _timer.Tick += value;
            remove => _timer.Tick -= value;
         }

         public void Start() => _timer.Start();

         public void StopTimer() => _timer.Stop();

         public void Dispose() => _timer.Stop();
      }
   }
}
