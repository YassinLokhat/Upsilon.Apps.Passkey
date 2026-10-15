using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Vault
{
   internal sealed partial class ServiceEditorView : ContentView
   {
      public ServiceEditorView()
      {
         InitializeComponent();
      }

      private void _runOnAccount(object? sender, Func<ServiceViewModel, ICommand> command)
      {
         if (BindingContext is not ServiceViewModel service
            || (sender as BindableObject)?.BindingContext is not AccountViewModel account)
         {
            return;
         }

         service.SelectedAccount = account;
         ICommand target = command(service);
         if (target.CanExecute(null))
         {
            target.Execute(null);
         }
      }

      private void _onMoveAccountUp(object? sender, EventArgs e) => _runOnAccount(sender, s => s.MoveAccountUpCommand);

      private void _onMoveAccountDown(object? sender, EventArgs e) => _runOnAccount(sender, s => s.MoveAccountDownCommand);

      private void _onDeleteAccount(object? sender, EventArgs e) => _runOnAccount(sender, s => s.DeleteAccountCommand);
   }
}
