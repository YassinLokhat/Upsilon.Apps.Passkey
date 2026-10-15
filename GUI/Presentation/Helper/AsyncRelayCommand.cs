using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Helper
{
   /// <summary>
   /// Asynchronous <see cref="ICommand"/>: <see cref="CanExecute"/> is false while
   /// a previous execution is still running, which replaces per-view busy guards.
   /// Notifies <see cref="CanExecuteChanged"/> itself (no WPF <c>CommandManager</c>).
   /// </summary>
   internal sealed class AsyncRelayCommand : ICommand
   {
      private readonly Func<object?, Task> _execute;
      private readonly Predicate<object?>? _canExecute;
      private bool _isRunning;

      public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
      {
         _execute = execute ?? throw new ArgumentNullException(nameof(execute));
         _canExecute = canExecute;
      }

      public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
         : this(_ => execute(), canExecute is null ? null : _ => canExecute())
      {
         ArgumentNullException.ThrowIfNull(execute);
      }

      public event EventHandler? CanExecuteChanged;

      public bool IsRunning => _isRunning;

      public bool CanExecute(object? parameter)
         => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

      // ICommand.Execute is void; exceptions surface on the UI synchronization
      // context, i.e. the host's unhandled-exception handler.
      public async void Execute(object? parameter)
         => await ExecuteAsync(parameter).ConfigureAwait(true);

      public async Task ExecuteAsync(object? parameter)
      {
         if (!CanExecute(parameter))
         {
            return;
         }

         _isRunning = true;
         RaiseCanExecuteChanged();
         try
         {
            await _execute(parameter).ConfigureAwait(true);
         }
         finally
         {
            _isRunning = false;
            RaiseCanExecuteChanged();
         }
      }

#pragma warning disable CA1030 // Raise* is the conventional ICommand re-query API name
      public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
#pragma warning restore CA1030
   }
}
