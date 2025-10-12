using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PasswordItem.xaml
   /// </summary>
   public partial class PasswordItem : UserControl
   {
      private readonly PasswordItemViewModel _viewModel;
      private readonly VisiblePasswordBox _password_VPB;

      public PasswordItem()
      {
         InitializeComponent();

         DataContext = _viewModel = new PasswordItemViewModel()
         {
            Index = 0,
            Password = string.Empty,
         };

         _password_VPB = (VisiblePasswordBox)FindName("Password");

         _viewModel.PropertyChanged += _viewModel_PropertyChanged;
         _password_VPB.Validated += _password_VPB_Validated;
      }

      private void _viewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Password"
            && _password_VPB.Password != _viewModel.Password)
         {
            _password_VPB.Password = _viewModel.Password;
         }
      }

      private void _password_VPB_Validated(object? sender, EventArgs e)
      {
         _viewModel.Password = _password_VPB.Password;
      }
   }
}
