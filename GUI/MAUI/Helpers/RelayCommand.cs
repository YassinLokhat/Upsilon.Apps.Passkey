using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   internal sealed class RelayCommand : ICommand
   {
      private readonly Action<object?> _execute;
      private readonly Predicate<object?>? _canExecute;
      private event EventHandler? _canExecuteChanged;

      public RelayCommand(Action execute, Func<bool>? canExecute = null)
         : this(_ => execute(), canExecute is null ? null : _ => canExecute())
      {
         ArgumentNullException.ThrowIfNull(execute);
      }

      public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
      {
         _execute = execute ?? throw new ArgumentNullException(nameof(execute));
         _canExecute = canExecute;
      }

      public event EventHandler? CanExecuteChanged
      {
         add => _canExecuteChanged += value;
         remove => _canExecuteChanged -= value;
      }

      public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

      public void Execute(object? parameter) => _execute(parameter);

      public static void RaiseCanExecuteChanged()
      {
         // Instance subscribers are raised via RaiseCanExecuteChanged on each command
         // when ViewModels call this; keep a no-op global for API parity with WPF.
      }

      public void NotifyCanExecuteChanged()
         => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
   }

   internal sealed class AsyncRelayCommand : ICommand
   {
      private readonly Func<object?, Task> _execute;
      private readonly Predicate<object?>? _canExecute;
      private bool _isRunning;
      private event EventHandler? _canExecuteChanged;

      public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
         : this(_ => execute(), canExecute is null ? null : _ => canExecute())
      {
         ArgumentNullException.ThrowIfNull(execute);
      }

      public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
      {
         _execute = execute ?? throw new ArgumentNullException(nameof(execute));
         _canExecute = canExecute;
      }

      public event EventHandler? CanExecuteChanged
      {
         add => _canExecuteChanged += value;
         remove => _canExecuteChanged -= value;
      }

      public bool CanExecute(object? parameter)
         => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

      public async void Execute(object? parameter)
      {
         if (!CanExecute(parameter))
         {
            return;
         }

         try
         {
            _isRunning = true;
            NotifyCanExecuteChanged();
            await _execute(parameter).ConfigureAwait(true);
         }
         finally
         {
            _isRunning = false;
            NotifyCanExecuteChanged();
         }
      }

      public void NotifyCanExecuteChanged()
         => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
   }
}
