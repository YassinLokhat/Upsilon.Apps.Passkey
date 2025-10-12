using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PrivateTextBox.xaml
   /// </summary>
   public partial class VisiblePasswordBox : UserControl
   {
      private readonly PasswordBox _passwordBox;
      private readonly VisiblePasswordBoxViewModel _visiblePasswordBoxViewModel;

      public string Password
      {
         get => _visiblePasswordBoxViewModel.Password;
         set => _visiblePasswordBoxViewModel.Password = value;
      }

      public event EventHandler? Validated;
      public event EventHandler? Aborded;

      public VisiblePasswordBox()
      {
         InitializeComponent();

         DataContext = _visiblePasswordBoxViewModel = new VisiblePasswordBoxViewModel();

         _passwordBox = (PasswordBox)this.FindName("PasswordBox");
         _passwordBox.PasswordChanged += _passwordBox_PasswordChanged;

         _passwordBox.KeyUp += _passwordBox_KeyUp;
         _passwordBox.LostFocus += _passwordBox_LostFocus;
      }

      private void _passwordBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
      {
         Validated?.Invoke(this, EventArgs.Empty);
      }

      private void _passwordBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            Validated?.Invoke(this, EventArgs.Empty);
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            Aborded?.Invoke(this, EventArgs.Empty);
         }
      }

      private void _passwordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
      {
         _visiblePasswordBoxViewModel.Password = _passwordBox.Password;
      }

      private void _viewButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _visiblePasswordBoxViewModel.TooglePasswordVisibility();
      }

      private void _viewButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _visiblePasswordBoxViewModel.TooglePasswordVisibility();
      }
   }
}
