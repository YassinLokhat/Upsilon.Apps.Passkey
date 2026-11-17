using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
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

      // Ticks the clipboard strip's countdown while a copied secret is pending.
      private readonly DispatcherTimer _clipboardTimer = new()
      {
         Interval = TimeSpan.FromMilliseconds(250),
      };

      internal string? GetAccountId() => _viewModel?.Account.ItemId;

      public AccountView()
      {
         InitializeComponent();

         _clipboardTimer.Tick += (_, _) => _refreshClipboardStrip();
         Loaded += (_, _) =>
         {
            ClipboardManager.PendingClearChanged += _clipboardManager_PendingClearChanged;
            _refreshClipboardStrip();
         };
         Unloaded += (_, _) =>
         {
            ClipboardManager.PendingClearChanged -= _clipboardManager_PendingClearChanged;
            _clipboardTimer.Stop();
         };
      }

      private void _clipboardManager_PendingClearChanged(object? sender, EventArgs e)
         => _refreshClipboardStrip();

      /// <summary>
      /// Shows "Copied. Clears in N s…" while the clipboard holds something we
      /// copied with an auto-clear, and hides the strip otherwise.
      /// </summary>
      private void _refreshClipboardStrip()
      {
         DateTime? pending = ClipboardManager.PendingClearUtc;

         if (pending is null)
         {
            _clipboard_G.Visibility = Visibility.Collapsed;
            _clipboardTimer.Stop();
            return;
         }

         int seconds = Math.Max(0, (int)Math.Ceiling((pending.Value - DateTime.UtcNow).TotalSeconds));
         _clipboard_TB.Text = Strings.Format(nameof(Strings.Msg_ClipboardClears), seconds);
         _clipboard_G.Visibility = Visibility.Visible;

         if (!_clipboardTimer.IsEnabled)
         {
            _clipboardTimer.Start();
         }
      }

      private void _clearClipboard_Click(object sender, RoutedEventArgs e)
         => ClipboardManager.ClearIfStillOwned();

      /// <summary>
      /// Identifier row "⋯": selects that row, then opens its menu (move up, move
      /// down, delete) bound to this account's identifier commands.
      /// </summary>
      private void _identifierMore_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel is null
            || sender is not Button { ContextMenu: { } menu } button)
         {
            return;
         }

         if (button.DataContext is IdentifierViewModel identifier)
         {
            _identifiers_LB.SelectedItem = identifier;
         }

         menu.DataContext = _viewModel;
         menu.PlacementTarget = button;
         menu.Placement = PlacementMode.Bottom;
         menu.IsOpen = true;
      }

      /// <summary>
      /// The account layer's info button shows the account Id as a tooltip and
      /// copies it on click (it used to be the frame header, copied on right-click).
      /// </summary>
      private void _copyAccountId_Click(object sender, RoutedEventArgs e)
      {
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

         // Identifiers.Count == 0 forces a rebuild (e.g. after Undo/Redo SyncFromModel).
         bool sameAccount = ReferenceEquals(_viewModel, dataContext) && dataContext.Identifiers.Count > 0;
         DataContext = _viewModel = dataContext;

         // Keep existing IdentifierViewModels (and their per-row baselines) when
         // re-selecting the same account after switching away.
         if (!sameAccount)
         {
            _viewModel.Identifiers.Clear();

            if (!_viewModel.Account.Identifiers.Any())
            {
               _ = _viewModel.AddIdentifier(string.Empty);
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

         QrCodeView.ShowQrCode(_password_VPB.Password,
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
