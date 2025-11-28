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

      public string? GetIdentifiant()
      {
         return _identifiants_LB.SelectedItem is not IdentifiantViewModel identifiantViewModel ? null : identifiantViewModel.Identifiant;
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
            _identifiants_LB.ItemsSource = null;

            return;
         }

         DataContext = _viewModel = dataContext;

         _viewModel.Identifiants.Clear();

         if (_viewModel.Account.Identifiants.Length == 0)
         {
            _viewModel.AddIdentifiant(string.Empty);
         }
         else
         {
            _viewModel.Identifiants = [.. _viewModel.Account.Identifiants.Select(x => new IdentifiantViewModel(_viewModel.Account, x))];
            foreach (IdentifiantViewModel identifiant in _viewModel.Identifiants)
            {
               _viewModel.AddIdentifiant(identifiant);
            }
         }

         _identifiants_LB.ItemsSource = _viewModel.Identifiants;
         _identifiants_LB.SelectedIndex = 0;

         _password_VPB.Password = _viewModel.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _passwords_LB.ItemsSource = _viewModel.Passwords;
      }

      private void _identifiant_DeleteClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int index = _identifiants_LB.SelectedIndex;

         if (_viewModel.RemoveIdentifiant((IdentifiantViewModel)_identifiants_LB.SelectedItem))
         {
            _identifiants_LB.SelectedIndex = index < _viewModel.Identifiants.Count ? index : _viewModel.Identifiants.Count - 1;
         }
      }

      private void _identifiant_UpClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiants_LB.SelectedIndex - 1;

         if (_viewModel.MoveIdentifiant(_identifiants_LB.SelectedIndex, newIndex))
         {
            _identifiants_LB.SelectedIndex = newIndex;
         }
      }

      private void _identifiant_DownClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiants_LB.SelectedIndex + 1;

         if (_viewModel.MoveIdentifiant(_identifiants_LB.SelectedIndex, newIndex))
         {
            _identifiants_LB.SelectedIndex = newIndex;
         }
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _viewModel?.AddIdentifiant(string.Empty);
         _identifiants_LB.SelectedIndex = _identifiants_LB.Items.Count - 1;
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

         passwordBox.Password = ((PasswordViewModel)((ContentPresenter)passwordBox.TemplatedParent).Content).Password;
      }

      private void _copyIdentifiant_Clicked(object sender, RoutedEventArgs e)
      {
         QrCodeHelper.CopyToClipboard(((IdentifiantViewModel)_identifiants_LB.SelectedItem).Identifiant);
      }

      private void _showQrCodeIdentifiant_Clicked(object sender, RoutedEventArgs e)
      {
         QrCodeHelper.ShowQrCode(((IdentifiantViewModel)_identifiants_LB.SelectedItem).Identifiant, MainViewModel.User.ShowPasswordDelay);
      }

      private void _copyPassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (_viewModel is null) return;
         QrCodeHelper.CopyToClipboard(_viewModel.Password);
      }

      private void _showQrCodePassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (_viewModel is null) return;
         QrCodeHelper.ShowQrCode(_viewModel.Password, MainViewModel.User.ShowPasswordDelay);
      }

      private void _copyPasswords_Clicked(object sender, RoutedEventArgs e)
      {
         if (sender is not Button button) return;
         QrCodeHelper.CopyToClipboard(((PasswordViewModel)((ContentPresenter)button.TemplatedParent).Content).Password);
      }

      private void _showQrCodePasswords_Clicked(object sender, RoutedEventArgs e)
      {
         if (sender is not Button button) return;
         QrCodeHelper.ShowQrCode(((PasswordViewModel)((ContentPresenter)button.TemplatedParent).Content).Password, MainViewModel.User.ShowPasswordDelay);
      }
   }
}
