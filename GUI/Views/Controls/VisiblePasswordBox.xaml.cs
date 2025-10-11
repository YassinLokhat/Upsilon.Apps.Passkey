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
      private readonly VisiblePasswordBoxViewModel _privateTextBoxViewModel;

      public VisiblePasswordBox()
      {
         InitializeComponent();

         DataContext = _privateTextBoxViewModel = new VisiblePasswordBoxViewModel();

         _passwordBox = (PasswordBox)this.FindName("PasswordBox");
         _passwordBox.PasswordChanged += _passwordBox_PasswordChanged;
      }

      private void _passwordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
      {
         _privateTextBoxViewModel.Password = _passwordBox.Password;
      }

      private void _viewButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _privateTextBoxViewModel.TooglePasswordVisibility();
      }

      private void _viewButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         _privateTextBoxViewModel.TooglePasswordVisibility();
      }
   }
}
