using System.ComponentModel;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PasswordItem.xaml
   /// </summary>
   public partial class PasswordItem : UserControl
   {
      public readonly PasswordItemViewModel ViewModel;
      private readonly VisiblePasswordBox _password_VPB;

      public event EventHandler? UpClicked;
      public event EventHandler? DownClicked;
      public event EventHandler? DeleteClicked;

      public PasswordItem(PasswordItemViewModel viewModel)
      {
         InitializeComponent();

         DataContext = ViewModel = viewModel;

         _password_VPB = (VisiblePasswordBox)FindName("Password");

         ViewModel.PropertyChanged += _viewModel_PropertyChanged;
         _password_VPB.Validated += _password_VPB_Validated;
      }

      private void _viewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Password"
            && _password_VPB.Password != ViewModel.Password)
         {
            _password_VPB.Password = ViewModel.Password;
         }
      }

      private void _password_VPB_Validated(object? sender, EventArgs e)
      {
         ViewModel.Password = _password_VPB.Password;
      }

      private void _upButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         UpClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _downButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         DownClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         DeleteClicked?.Invoke(this, EventArgs.Empty);
      }
   }
}
