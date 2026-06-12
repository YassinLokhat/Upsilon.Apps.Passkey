using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Asynchronous variant of <see cref="RelayCommand"/>. The command reports
   /// <see cref="CanExecute"/> as <c>false</c> while the previous execution is
   /// still pending, preventing re-entrancy.
   /// </summary>
   public sealed class AsyncRelayCommand : ICommand
   {
      private readonly Func<object?, Task> _execute;
      private readonly Predicate<object?>? _canExecute;
      private bool _isRunning;

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
         add => CommandManager.RequerySuggested += value;
         remove => CommandManager.RequerySuggested -= value;
      }

      public bool IsRunning => _isRunning;

      public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

      public async void Execute(object? parameter)
      {
         if (!CanExecute(parameter))
         {
            return;
         }

         _isRunning = true;
         CommandManager.InvalidateRequerySuggested();

         try
         {
            await _execute(parameter).ConfigureAwait(true);
         }
         finally
         {
            _isRunning = false;
            CommandManager.InvalidateRequerySuggested();
         }
      }
   }
}
