using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PrivateTextBox.xaml
   /// </summary>
   public partial class VisiblePasswordBox : UserControl
   {
      private readonly VisiblePasswordBoxViewModel _viewModel;

      public string Password
      {
         get => _viewModel.Password;
         set => _viewModel.Password = value;
      }

      public event EventHandler? Validated;
      public event EventHandler? Aborded;

      public VisiblePasswordBox()
      {
         InitializeComponent();

         DataContext = _viewModel = new VisiblePasswordBoxViewModel();

         _viewModel.PropertyChanged += _viewModel_PropertyChanged;

         _passwordBox.PasswordChanged += _passwordBox_PasswordChanged;
         _passwordBox.KeyUp += _passwordBox_KeyUp;
      }

      private void _viewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if ((e.PropertyName == nameof(Password))
            && _passwordBox.Password != Password)
         {
            _passwordBox.Password = Password;
         }
      }

      private void _passwordBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Escape)
         {
            Password = string.Empty;
            Aborded?.Invoke(this, EventArgs.Empty);
         }
      }

      private void _passwordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
      {
         _viewModel.Password = _passwordBox.Password;
         Validated?.Invoke(this, EventArgs.Empty);
      }

      private void _viewButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _viewModel.TooglePasswordVisibility();
      }

      private void _viewButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _viewModel.TooglePasswordVisibility();
      }
   }
}
