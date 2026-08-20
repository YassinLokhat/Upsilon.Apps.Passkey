using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
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

      public event EventHandler? UpClicked;
      public event EventHandler? DownClicked;
      public event EventHandler? DeleteClicked;

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

      private void _upButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         UpClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _downButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         DownClicked?.Invoke(this, EventArgs.Empty);
      }

      private void _deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         DeleteClicked?.Invoke(this, EventArgs.Empty);
      }
   }
}
