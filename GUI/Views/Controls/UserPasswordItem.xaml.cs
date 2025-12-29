using System.ComponentModel;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for UserPasswordItem.xaml
   /// </summary>
   public partial class UserPasswordItem : UserControl
   {
      public readonly UserPasswordItemViewModel ViewModel;

      public event EventHandler? UpClicked;
      public event EventHandler? DownClicked;
      public event EventHandler? DeleteClicked;

      public UserPasswordItem(UserPasswordItemViewModel viewModel)
      {
         InitializeComponent();

         DataContext = ViewModel = viewModel;
         _password_VPB.Password = ViewModel.Password;

         ViewModel.PropertyChanged += _viewModel_PropertyChanged;
         _password_VPB.PasswordChanged += _password_VPB_PasswordChanged;
      }

      private void _viewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Password"
            && _password_VPB.Password != ViewModel.Password)
         {
            _password_VPB.Password = ViewModel.Password;
         }
      }

      private void _password_VPB_PasswordChanged(object? sender, EventArgs e)
      {
         ViewModel.Password = _password_VPB.Password;
      }

      private void _upButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         UpClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _downButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         DownClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         DeleteClicked?.Invoke(this, EventArgs.Empty);
      }
   }
}
