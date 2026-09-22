using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for UserPasswordItem.xaml
   /// </summary>
   internal sealed partial class UserPasswordItem : UserControl
   {
      internal readonly UserPasswordItemViewModel ViewModel;

      /// <summary>
      /// Reads the secret from the PasswordBox rather than a ViewModel string,
      /// so typing does not leave a long-lived managed duplicate.
      /// </summary>
      public string Password
      {
         get => _password_VPB.Password;
         set => _password_VPB.Password = value;
      }

      internal UserPasswordItem(UserPasswordItemViewModel viewModel)
      {
         InitializeComponent();

         DataContext = ViewModel = viewModel;
         _password_VPB.Password = viewModel.InitialPassword;
         // Drop the seed copy now that PasswordBox holds it.
         viewModel.InitialPassword = string.Empty;
      }

      public new void Focus()
      {
         _password_VPB.Focus();
      }

      public void Clear() => _password_VPB.Clear();
   }
}
