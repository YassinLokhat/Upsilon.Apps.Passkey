using System.Security;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
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

         _ = _username_TB.Focus();

         _username_TB.KeyUp += _username_TB_KeyUp;
         _password_PB.KeyUp += _password_PB_KeyUp;

         Loaded += (s, e) => this.PostLoadSetup();
         Closed += (s, e) =>
         {
            _clearInputs();
            _viewModel.ReleaseExpectedCredentials();
         };
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
            string username = _username_TB.Text;
            _username_TB.Text = string.Empty;

            if (_viewModel.ValidateCredentials(username))
            {
               DialogResult = true;
               return;
            }

            _ = _password_PB.Focus();
            _clearInputs();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            // Escape resets a poisoned / in-progress sequence; it does not close the dialog.
            _clearCredentials();
         }
      }

      private void _password_PB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.SystemKey != System.Windows.Input.Key.None)
         {
            return;
         }

         if (e.Key == System.Windows.Input.Key.Enter)
         {
            // PasswordBox.SecurePassword returns a new SecureString the caller must dispose.
            using SecureString securePassword = _password_PB.SecurePassword;
            if (securePassword.Length == 0)
            {
               return;
            }

            // UseAsString zeroes the unmanaged BSTR; Clear erases the PasswordBox buffer.
            bool complete = securePassword.UseAsString(_viewModel.ValidateCredentials);
            _password_PB.Clear();

            if (complete)
            {
               DialogResult = true;
               return;
            }

            _clearInputs();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            // Escape resets a poisoned / in-progress sequence; it does not close the dialog.
            _clearCredentials();
         }
      }

      private void _clearInputs()
      {
         _username_TB.Text = string.Empty;
         _password_PB.Clear();
      }

      private void _clearCredentials()
      {
         // Intentional restart after poison (login parity); cancel is close / X only.
         _viewModel.ClearCredentials();
         _ = _username_TB.Focus();
         _clearInputs();
      }
   }
}
