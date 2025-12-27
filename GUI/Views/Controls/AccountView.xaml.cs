using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountView.xaml
   /// </summary>
   public partial class AccountView : UserControl
   {
      private AccountViewModel? _viewModel;

      public AccountView()
      {
         InitializeComponent();
      }

      public string? GetIdentifier()
      {
         return _identifiers_LB.SelectedItem is not IdentifierViewModel identifierViewModel ? null : identifierViewModel.Identifier;
      }

      public string? GetPassword() => _viewModel?.Password;

      public void SetPassword(string password)
      {
         if (_viewModel is null) return;

         _viewModel.Password = password;

         _password_VPB.Password = _viewModel.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _passwords_LB.ItemsSource = _viewModel.Passwords;
      }

      public void SetDataContext(AccountViewModel? dataContext)
      {
         if (dataContext is null)
         {
            DataContext = null;
            _viewModel = null;
            _identifiers_LB.ItemsSource = null;

            return;
         }

         DataContext = _viewModel = dataContext;

         _viewModel.Identifiers.Clear();

         if (_viewModel.Account.Identifiers.Length == 0)
         {
            _viewModel.AddIdentifier(string.Empty);
         }
         else
         {
            _viewModel.Identifiers = [.. _viewModel.Account.Identifiers.Select(x => new IdentifierViewModel(_viewModel.Account, x))];
            foreach (IdentifierViewModel identifier in _viewModel.Identifiers)
            {
               _viewModel.AddIdentifier(identifier);
            }
         }

         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         _identifiers_LB.SelectedIndex = 0;

         _password_VPB.Password = _viewModel.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _passwords_LB.ItemsSource = _viewModel.Passwords;
      }

      private void _identifier_DeleteClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int index = _identifiers_LB.SelectedIndex;

         if (_viewModel.RemoveIdentifier((IdentifierViewModel)_identifiers_LB.SelectedItem))
         {
            _identifiers_LB.SelectedIndex = index < _viewModel.Identifiers.Count ? index : _viewModel.Identifiers.Count - 1;
         }
      }

      private void _identifier_UpClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiers_LB.SelectedIndex - 1;

         if (_viewModel.MoveIdentifier(_identifiers_LB.SelectedIndex, newIndex))
         {
            _identifiers_LB.SelectedIndex = newIndex;
         }
      }

      private void _identifier_DownClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiers_LB.SelectedIndex + 1;

         if (_viewModel.MoveIdentifier(_identifiers_LB.SelectedIndex, newIndex))
         {
            _identifiers_LB.SelectedIndex = newIndex;
         }
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _viewModel?.AddIdentifier(string.Empty);
         _identifiers_LB.SelectedIndex = _identifiers_LB.Items.Count - 1;
      }

      private void _value_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         NumericTextBoxHelper.PreviewTextInput(sender, e);
      }

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         NumericTextBoxHelper.Pasting(sender, e);
      }

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         NumericTextBoxHelper.TextChanged(sender, e);
      }

      private void _password_VPB_Validated(object sender, EventArgs e)
      {
         if (_viewModel is null) return;

         _viewModel.Password = _password_VPB.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _passwords_LB.ItemsSource = _viewModel.Passwords;
      }

      private void _passwords_VPB_Loaded(object sender, RoutedEventArgs e)
      {
         if (sender is not VisiblePasswordBox passwordBox) return;

         try
         {
            passwordBox.Password = ((PasswordViewModel)((ContentPresenter)passwordBox.TemplatedParent).Content).Password;
         }
         catch { }
      }

      private void _copyIdentifier_Clicked(object sender, RoutedEventArgs e)
      {
         QrCodeView.CopyToClipboard(((IdentifierViewModel)_identifiers_LB.SelectedItem).Identifier);
      }

      private void _showQrCodeIdentifier_Clicked(object sender, RoutedEventArgs e)
      {
         QrCodeView.ShowQrCode(Window.GetWindow(this),
            ((IdentifierViewModel)_identifiers_LB.SelectedItem).Identifier,
            MainViewModel.User.ShowPasswordDelay);
      }

      private void _copyPassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (_viewModel is null) return;
         QrCodeView.CopyToClipboard(_viewModel.Password);
      }

      private void _showQrCodePassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (_viewModel is null) return;
         QrCodeView.ShowQrCode(Window.GetWindow(this),
            _viewModel.Password,
            MainViewModel.User.ShowPasswordDelay);
      }

      private void _copyPasswords_Clicked(object sender, RoutedEventArgs e)
      {
         if (sender is not Button button) return;
         QrCodeView.CopyToClipboard(((PasswordViewModel)((ContentPresenter)button.TemplatedParent).Content).Password);
      }

      private void _showQrCodePasswords_Clicked(object sender, RoutedEventArgs e)
      {
         if (sender is not Button button) return;
         QrCodeView.ShowQrCode(Window.GetWindow(this),
            ((PasswordViewModel)((ContentPresenter)button.TemplatedParent).Content).Password,
            MainViewModel.User.ShowPasswordDelay);
      }
   }
}
