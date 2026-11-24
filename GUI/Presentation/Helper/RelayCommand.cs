using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Helper
{
   /// <summary>
   /// Portable <see cref="ICommand"/> that raises <see cref="CanExecuteChanged"/> itself
   /// (no WPF <c>CommandManager</c>).
   /// </summary>
   internal sealed class RelayCommand : ICommand
   {
      private readonly Action<object?> _execute;
      private readonly Predicate<object?>? _canExecute;

      public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
      {
         _execute = execute ?? throw new ArgumentNullException(nameof(execute));
         _canExecute = canExecute;
      }

      public RelayCommand(Action execute, Func<bool>? canExecute = null)
         : this(_ => execute(), canExecute is null ? null : _ => canExecute())
      {
         ArgumentNullException.ThrowIfNull(execute);
      }

      public event EventHandler? CanExecuteChanged;

      public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

      public void Execute(object? parameter) => _execute(parameter);

#pragma warning disable CA1030 // Raise* is the conventional ICommand re-query API name
      public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

      public static void NotifyCanExecuteChanged(params RelayCommand[] commands)
      {
         ArgumentNullException.ThrowIfNull(commands);
         foreach (RelayCommand command in commands)
         {
            command.RaiseCanExecuteChanged();
         }
      }
#pragma warning restore CA1030
   }
}
