using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for CredentialsConfirmationView.xaml
   /// </summary>
   internal sealed partial class CredentialsConfirmationView : Window
   {
      private readonly CredentialsConfirmationViewModel _viewModel;

      private CredentialsConfirmationView(IEnumerable<string> credentials, bool isNew)
      {
         InitializeComponent();

         DataContext = _viewModel = new CredentialsConfirmationViewModel(credentials, isNew);

         _username_TB.Focus();

         _username_TB.KeyUp += _username_TB_KeyUp;
         _password_PB.KeyUp += _password_PB_KeyUp;
      }

      public static bool ShowConfirmationDialog(IEnumerable<string> credentials, bool isNew)
         => new CredentialsConfirmationView(credentials, isNew).ShowDialog() ?? false;

      private void _username_TB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.SystemKey != System.Windows.Input.Key.None)
         {
            return;
         }

         if (e.Key == System.Windows.Input.Key.Enter
            && !string.IsNullOrWhiteSpace(_username_TB.Text))
         {
            if (_viewModel.ValidateCredentials(_username_TB.Text))
            {
               DialogResult = true;
               return;
            }

            _password_PB.Focus();
            _clearInputs();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            _clearCredentials();
         }
      }

      private void _password_PB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.SystemKey != System.Windows.Input.Key.None)
         {
            return;
         }

         if (e.Key == System.Windows.Input.Key.Enter
            && !string.IsNullOrWhiteSpace(_password_PB.Password))
         {
            if (_viewModel.ValidateCredentials(_password_PB.Password))
            {
               DialogResult = true;
               return;
            }

            _clearInputs();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            _clearCredentials();
         }
      }

      private void _clearInputs()
      {
         _username_TB.Text = string.Empty;
         _password_PB.Password = string.Empty;
      }

      private void _clearCredentials()
      {
         _viewModel.ClearCredentials();
         _username_TB.Focus();
         _clearInputs();
      }
   }
}
