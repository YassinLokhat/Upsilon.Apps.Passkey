using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for PrivateTextBox.xaml
   /// </summary>
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by WPF via XAML/BAML.")]
   internal sealed partial class VisiblePasswordBox : UserControl
   {
      private readonly VisiblePasswordBoxViewModel _viewModel;

      /// <summary>
      /// Reads from / writes to the underlying <see cref="PasswordBox"/>. The
      /// ViewModel only holds a plaintext copy while the password is revealed.
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

            // Keep the reveal TextBox in sync only while it is visible.
            if (_viewModel.TextVisibility == Visibility.Visible)
            {
               _viewModel.RevealText = value;
            }
         }
      }

      public bool ReadOnly
      {
         get => !_viewModel.IsEnabled;
         set => _viewModel.IsEnabled = !value;
      }

      public Brush BackgroundColor
      {
         get => _viewModel.Background;
         set => _viewModel.Background = value;
      }

      public event EventHandler? PasswordChanged;
      public event EventHandler? Validated;
      public event EventHandler? Aborded;

      public VisiblePasswordBox()
      {
         InitializeComponent();

         DataContext = _viewModel = new VisiblePasswordBoxViewModel();

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
      public void Clear()
      {
         _viewModel.HidePassword();
         _viewModel.RevealText = string.Empty;
         _passwordBox.Clear();
      }

      private void _passwordBox_LostFocus(object sender, RoutedEventArgs e)
      {
         Validated?.Invoke(this, EventArgs.Empty);
      }

      private void _passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
      {
         // Do not mirror into a managed string while the password stays masked;
         // only refresh the reveal TextBox when it is currently shown.
         if (_viewModel.TextVisibility == Visibility.Visible)
         {
            _viewModel.RevealText = _passwordBox.Password;
         }

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
               Clear();
               Aborded?.Invoke(this, EventArgs.Empty);
               break;
         }
      }

      private void _viewButton_MouseDown(object sender, MouseButtonEventArgs e)
      {
         // Materialize plaintext only for the duration of the press-and-hold reveal.
         _viewModel.RevealText = _passwordBox.Password;
         _viewModel.ShowPassword();
      }

      private void _viewButton_MouseUp(object sender, MouseButtonEventArgs e)
      {
         _viewModel.HidePassword();
         _viewModel.RevealText = string.Empty;
      }
   }
}
