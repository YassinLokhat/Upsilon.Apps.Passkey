using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   /// <summary>
   /// The "Copied. Clears in N s…" strip under the password: shown while
   /// something copied with an auto-clear is waiting to be cleared, with a
   /// countdown ticking four times a second. The view only animates it.
   /// </summary>
   internal sealed class ClipboardStatusViewModel : ObservableObject
   {
      private readonly Func<DateTime?> _pendingClearUtc;
      private readonly Func<DateTime> _utcNow;
      private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
      private bool _listening;

      public ClipboardStatusViewModel()
         : this(static () => ClipboardManager.PendingClearUtc, static () => DateTime.UtcNow)
      {
      }

      /// <summary>Test seam: where the pending clear time and the clock come from.</summary>
      internal ClipboardStatusViewModel(Func<DateTime?> pendingClearUtc, Func<DateTime> utcNow)
      {
         _pendingClearUtc = pendingClearUtc;
         _utcNow = utcNow;
         _timer.Tick += (_, _) => Refresh();
         ClearCommand = new RelayCommand(ClipboardManager.ClearIfStillOwned);
      }

      /// <summary>Raised when a new copy starts a countdown (the view restarts its motion).</summary>
      public event EventHandler? CountdownStarted;

      public ICommand ClearCommand { get; }

      public bool IsActive
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public string Text
      {
         get;
         private set => SetProperty(ref field, value);
      } = string.Empty;

      /// <summary>Time left until the clear, when <see cref="IsActive"/>.</summary>
      public TimeSpan Remaining { get; private set; }

      // The pending clear the strip currently shows (a different one = a new copy).
      private DateTime? _shownPendingClear;

      /// <summary>Starts following the clipboard (view loaded).</summary>
      public void Start()
      {
         if (!_listening)
         {
            ClipboardManager.PendingClearChanged += _onPendingClearChanged;
            _listening = true;
         }

         Refresh();
      }

      /// <summary>Stops following the clipboard (view unloaded).</summary>
      public void Stop()
      {
         if (_listening)
         {
            ClipboardManager.PendingClearChanged -= _onPendingClearChanged;
            _listening = false;
         }

         _timer.Stop();
      }

      public void Refresh()
      {
         DateTime? pending = _pendingClearUtc();
         if (pending is null)
         {
            _timer.Stop();
            _shownPendingClear = null;
            IsActive = false;
            return;
         }

         TimeSpan remaining = pending.Value - _utcNow();
         Remaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
         Text = Strings.Format(nameof(Strings.Msg_ClipboardClears), Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)));

         bool newCopy = _shownPendingClear != pending;
         _shownPendingClear = pending;
         IsActive = true;
         if (newCopy)
         {
            CountdownStarted?.Invoke(this, EventArgs.Empty);
         }

         if (!_timer.IsEnabled)
         {
            _timer.Start();
         }
      }

      private void _onPendingClearChanged(object? sender, EventArgs e) => Refresh();
   }
}
