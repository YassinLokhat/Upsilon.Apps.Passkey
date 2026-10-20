using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Adapters;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for PrivateTextBox.xaml
   /// </summary>
   internal sealed partial class VisiblePasswordBox : UserControl
   {
      private readonly SecretField _secret;

      /// <summary>
      /// Reads from / writes to the underlying <see cref="PasswordBox"/>. The
      /// reveal TextBox only holds a plaintext copy while the password is revealed.
      /// </summary>
      public string Password
      {
         get => _passwordBox.Password;
         set
         {
            if (_passwordBox.Password != value)
            {
               _passwordBox.Password = value;
            }

            _secret.RefreshMirror();
         }
      }

      public bool ReadOnly
      {
         get => !_secret.IsEnabled;
         set => _secret.IsEnabled = !value;
      }

      /// <summary>
      /// Forwards to <see cref="Control.Background"/> so code-behind and XAML
      /// bindings both paint the inner PasswordBox / reveal TextBox.
      /// </summary>
      public Brush BackgroundColor
      {
         get => Background;
         set => Background = value;
      }

      public event EventHandler? PasswordChanged;
      public event EventHandler? Validated;
      public event EventHandler? Aborted;

      public VisiblePasswordBox()
      {
         InitializeComponent();
         _secret = new SecretField(new PasswordBoxSecretInput(_passwordBox), mirrorsPlaintext: true);

         // Keep DataContext on the inner tree only: the UserControl must inherit
         // the parent DataContext so attributes like Background="{Binding …}" resolve.
         _root.DataContext = _secret;

         _passwordBox.LostFocus += _passwordBox_LostFocus;
         _passwordBox.KeyUp += _passwordBox_KeyUp;
         _passwordBox.PasswordChanged += _passwordBox_PasswordChanged;
      }

      public new void Focus()
      {
         _ = _passwordBox.Focus();
      }

      /// <summary>
      /// Clears both the PasswordBox buffer and any revealed plaintext copy.
      /// </summary>
      public void Clear() => _secret.Clear();

      private void _passwordBox_LostFocus(object sender, RoutedEventArgs e)
      {
         Validated?.Invoke(this, EventArgs.Empty);
      }

      private void _passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
      {
         _secret.RefreshMirror();
         PasswordChanged?.Invoke(this, EventArgs.Empty);
      }

      private void _passwordBox_KeyUp(object sender, KeyEventArgs e)
      {
         switch (e.Key)
         {
            case Key.Enter:
               Validated?.Invoke(this, EventArgs.Empty);
               break;
            case Key.Escape:
               if (ReadOnly)
               {
                  break;
               }

               // Clear the edit buffer, then let the host restore the committed value.
               Clear();
               Aborted?.Invoke(this, EventArgs.Empty);
               break;
         }
      }

      private void _viewButton_MouseDown(object sender, MouseButtonEventArgs e) => _secret.Reveal();

      private void _viewButton_MouseUp(object sender, MouseButtonEventArgs e) => _secret.Mask();
   }
}
