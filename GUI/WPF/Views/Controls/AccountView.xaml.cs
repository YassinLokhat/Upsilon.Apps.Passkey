using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountView.xaml
   /// </summary>
   internal sealed partial class AccountView : UserControl
   {
      private AccountViewModel? _viewModel;

      internal string? GetAccountId() => _viewModel?.Account.ItemId;

      public AccountView()
      {
         InitializeComponent();
      }

      private void _accountView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
      {
         string sourceText = (e.OriginalSource as TextBlock)?.Text ?? string.Empty;

         if (sourceText != _account_GB.Header.ToString())
         {
            return;
         }

         string? itemId = GetAccountId();

         if (itemId is null)
         {
            return;
         }

         AppServices.Clipboard.SetText(itemId);

         e.Handled = true;
      }

      public string? GetIdentifier()
         => _identifiers_LB.SelectedItem is IdentifierViewModel identifierViewModel
            ? identifierViewModel.Identifier
            : null;

      public string? Password
      {
         get => _password_VPB.Password;
         set
         {
            ArgumentNullException.ThrowIfNull(value);

            if (_viewModel is null)
            {
               return;
            }

            _viewModel.Password = value;

            _password_VPB.Password = value;
            _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
            _refreshPasswordHistory();
         }
      }

      internal void SetDataContext(AccountViewModel? dataContext)
      {
         // Drop any previously revealed / mirrored secrets before switching.
         _clearSecrets();

         if (dataContext is null)
         {
            DataContext = null;
            _viewModel = null;
            _identifiers_LB.ItemsSource = null;

            return;
         }

         bool sameAccount = ReferenceEquals(_viewModel, dataContext) && dataContext.Identifiers.Count > 0;
         DataContext = _viewModel = dataContext;

         // Keep existing IdentifierViewModels (and their per-row baselines) when
         // re-selecting the same account after switching away.
         if (!sameAccount)
         {
            _viewModel.Identifiers.Clear();

            if (!_viewModel.Account.Identifiers.Any())
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
         }

         _identifiers_LB.ItemsSource = _viewModel.Identifiers;
         if (_identifiers_LB.SelectedIndex < 0 && _viewModel.Identifiers.Count > 0)
         {
            _identifiers_LB.SelectedIndex = 0;
         }

         _password_VPB.Password = _viewModel.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _refreshPasswordHistory();
      }

      private void _clearSecrets()
      {
         _password_VPB.Clear();

         if (_passwords_LB.ItemsSource is IEnumerable<PasswordViewModel> history)
         {
            foreach (PasswordViewModel entry in history)
            {
               entry.Clear();
            }
         }

         _passwords_LB.ItemsSource = null;
      }

      private void _refreshPasswordHistory()
      {
         if (_passwords_LB.ItemsSource is IEnumerable<PasswordViewModel> previous)
         {
            foreach (PasswordViewModel entry in previous)
            {
               entry.Clear();
            }
         }

         _passwords_LB.ItemsSource = _viewModel?.Passwords;
      }

      private void _value_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         _viewModel.Password = _password_VPB.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
         _refreshPasswordHistory();
      }

      private void _password_VPB_Aborted(object sender, EventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         // Escape cancels the in-progress edit: restore the committed password
         // and its dirty/leak background instead of leaving an empty buffer.
         _password_VPB.Password = _viewModel.Password;
         _password_VPB.BackgroundColor = _viewModel.PasswordBackground;
      }

      private void _passwords_VPB_Loaded(object sender, RoutedEventArgs e)
      {
         if (sender is not VisiblePasswordBox passwordBox)
         {
            return;
         }

         try
         {
            passwordBox.Password = ((PasswordViewModel)((ContentPresenter)passwordBox.TemplatedParent).Content).Password;
         }
         catch (InvalidCastException)
         {
            System.Diagnostics.Trace.TraceWarning("Loading password view failed");
         }
      }

      private void _copyPassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         AppServices.Clipboard.SetText(_password_VPB.Password,
          ClipboardManager.AutoClearAfter);
      }

      private void _showQrCodePassword_Clicked(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         QrCodeView.ShowQrCode(Window.GetWindow(this),
            _password_VPB.Password,
            AppServices.Session.User?.Settings.ShowPasswordDelay ?? 0);
      }

      private void _identifiers_LB_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
      {
         // Focusing a TextBox/ComboBox inside a row does not always select that ListBoxItem;
         // sync so Copy / QR / Ctrl+Shift+L / move / delete use the focused identifier.
         if (_identifiers_LB.ContainerFromElement(e.NewFocus as DependencyObject) is ListBoxItem item)
         {
            item.IsSelected = true;
         }
      }

      private void _identifier_TextBox_KeyUp(object sender, KeyEventArgs e)
      {
         if (this.GetIsBusy()
            || e.Key is not Key.Enter
            || sender is not FrameworkElement element
            || element.DataContext is not IdentifierViewModel viewModel)
         {
            return;
         }

         InsertIdentifierViewModel.ShowInsertIdentifierView(viewModel);
      }
   }
}
