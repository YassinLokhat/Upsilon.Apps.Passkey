using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Adapters;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using CredentialsConfirmationViewModel = Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools.CredentialsConfirmationViewModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for CredentialsConfirmationView.xaml
   /// </summary>
   internal sealed partial class CredentialsConfirmationView : Window, ILanguageAware
   {
      private readonly CredentialsConfirmationViewModel _viewModel;
      private readonly PasswordBoxSecretInput _passwordInput;

      private CredentialsConfirmationView(IEnumerable<string> credentials, bool isNew)
      {
         InitializeComponent();
         _passwordInput = new PasswordBoxSecretInput(_password_PB);

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

      public void OnLanguageChanged()
         => _viewModel.OnLanguageChanged();

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
            string password = _passwordInput.Read();
            if (password.Length == 0)
            {
               return;
            }

            _passwordInput.Clear();
            bool complete = _viewModel.ValidateCredentials(password);

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
