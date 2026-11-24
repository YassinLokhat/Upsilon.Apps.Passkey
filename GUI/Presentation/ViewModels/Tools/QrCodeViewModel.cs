using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools
{
   /// <summary>
   /// QR code of a secret shown for a limited time. Hosts render <see cref="Modules"/>
   /// and close the view on <see cref="Expired"/>. The source text is not retained.
   /// </summary>
   internal sealed class QrCodeViewModel : ObservableObject
   {
      private IUiTimer? _timer;

      /// <param name="content">Text to encode.</param>
      /// <param name="delayMilliseconds">Display duration; zero or negative means no expiry.</param>
      /// <param name="timers">Timer factory; defaults to <see cref="AppServices.Timers"/>.</param>
      public QrCodeViewModel(string content, int delayMilliseconds, IUiTimerFactory? timers = null)
      {
         ArgumentException.ThrowIfNullOrEmpty(content);

         Modules = QrCode.Generate(content);
         Title = _title();

         if (delayMilliseconds > 0
            && (timers ?? AppServices.Timers) is { } factory)
         {
            _timer = factory.Create();
            _timer.Interval = TimeSpan.FromMilliseconds(delayMilliseconds);
            _timer.Tick += _timer_Tick;
            _timer.Start();
         }
      }

      /// <summary>Module matrix, <c>[row][column]</c>; empty once expired or closed.</summary>
      public bool[][] Modules
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public int Size => Modules.Length;

      public bool IsClosed { get; private set; }

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      }

      /// <summary>Raised once when the display delay elapses.</summary>
      public event EventHandler? Expired;

      public void OnLanguageChanged() => Title = _title();

      /// <summary>Stops the timer and clears the matrix; idempotent.</summary>
      public void Close()
      {
         if (IsClosed)
         {
            return;
         }

         IsClosed = true;
         _stopTimer();

         foreach (bool[] row in Modules)
         {
            Array.Clear(row);
         }

         Modules = [];
         OnPropertyChanged(nameof(Size));
      }

      private void _timer_Tick(object? sender, EventArgs e)
      {
         if (IsClosed)
         {
            return;
         }

         Close();
         Expired?.Invoke(this, EventArgs.Empty);
      }

      private void _stopTimer()
      {
         if (_timer is null)
         {
            return;
         }

         _timer.Tick -= _timer_Tick;
         _timer.StopTimer();
         _timer.Dispose();
         _timer = null;
      }

      private static string _title()
         => Strings.Format(nameof(Strings.Title_QrCode), AppServices.Info.Title);
   }
}
