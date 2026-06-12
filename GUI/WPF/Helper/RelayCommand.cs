using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Lightweight <see cref="ICommand"/> implementation that forwards execution
   /// to the supplied delegates. Hooks into <see cref="CommandManager.RequerySuggested"/>
   /// so WPF automatically re-evaluates <see cref="CanExecute"/>.
   /// </summary>
   public sealed class RelayCommand : ICommand
   {
      private readonly Action<object?> _execute;
      private readonly Predicate<object?>? _canExecute;

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
         add => CommandManager.RequerySuggested += value;
         remove => CommandManager.RequerySuggested -= value;
      }

      public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

      public void Execute(object? parameter) => _execute(parameter);

      /// <summary>
      /// Manually requests a re-evaluation of every command's <see cref="CanExecute"/>.
      /// </summary>
      public static void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
   }
}
