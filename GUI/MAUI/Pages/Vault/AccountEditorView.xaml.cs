using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Vault
{
   internal sealed partial class AccountEditorView : ContentView
   {
      public AccountEditorView()
      {
         InitializeComponent();
      }

      private AccountViewModel? _account => BindingContext as AccountViewModel;

      protected override void OnBindingContextChanged()
      {
         base.OnBindingContextChanged();
         PasswordEntry.IsPassword = true;

         // An empty list also follows Undo/Redo SyncFromModel, which drops the rows.
         if (_account is { Identifiers.Count: 0 } account)
         {
            account.LoadIdentifiers();
         }
      }

      private void _onRevealPressed(object? sender, EventArgs e) => PasswordEntry.IsPassword = false;

      private void _onRevealReleased(object? sender, EventArgs e) => PasswordEntry.IsPassword = true;

      private void _onCopyPassword(object? sender, EventArgs e)
      {
         if (!string.IsNullOrEmpty(_account?.Password))
         {
            AppServices.Clipboard?.SetText(_account.Password, ClipboardPolicy.AutoClearAfter);
         }
      }

      private void _runOnIdentifier(object? sender, Func<AccountViewModel, System.Windows.Input.ICommand> command)
      {
         if (_account is not { } account
            || (sender as BindableObject)?.BindingContext is not IdentifierViewModel identifier)
         {
            return;
         }

         account.SelectedIdentifier = identifier;
         System.Windows.Input.ICommand target = command(account);
         if (target.CanExecute(null))
         {
            target.Execute(null);
         }
      }

      private void _onCopyIdentifier(object? sender, EventArgs e) => _runOnIdentifier(sender, a => a.CopyIdentifierCommand);

      private void _onMoveIdentifierUp(object? sender, EventArgs e) => _runOnIdentifier(sender, a => a.MoveIdentifierUpCommand);

      private void _onMoveIdentifierDown(object? sender, EventArgs e) => _runOnIdentifier(sender, a => a.MoveIdentifierDownCommand);

      private void _onDeleteIdentifier(object? sender, EventArgs e) => _runOnIdentifier(sender, a => a.DeleteIdentifierCommand);

      private async void _onEditIdentifier(object? sender, TappedEventArgs e)
      {
         if ((sender as BindableObject)?.BindingContext is IdentifierViewModel identifier)
         {
            await AccountViewModel.EditIdentifierAsync(identifier).ConfigureAwait(true);
         }
      }
   }
}
