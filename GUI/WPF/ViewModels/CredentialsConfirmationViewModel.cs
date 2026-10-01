using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class CredentialsConfirmationViewModel(IEnumerable<string> credentials, bool isNew) : ObservableObject, ILanguageAware
   {
      private readonly bool _isNew = isNew;
      private readonly IEnumerable<string> _realCredentials = credentials;
      private readonly List<string> _credentials = [];

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = isNew ? Strings.Title_NewCredentialsConfirmation : Strings.Title_OldCredentialsConfirmation;

      public bool IsAwaitingPasskeys
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(CredentialsLabel));
               OnPropertyChanged(nameof(UsernameVisibility));
               OnPropertyChanged(nameof(PasswordVisibility));
            }
         }
      }

      public string CredentialsLabel => IsAwaitingPasskeys ? Strings.Label_Password : Strings.Label_Username;
      public System.Windows.Visibility UsernameVisibility => IsAwaitingPasskeys ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
      public System.Windows.Visibility PasswordVisibility => IsAwaitingPasskeys ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

      public void OnLanguageChanged()
      {
         Title = _isNew ? Strings.Title_NewCredentialsConfirmation : Strings.Title_OldCredentialsConfirmation;
         OnPropertyChanged(nameof(CredentialsLabel));
      }

      public void ClearCredentials()
      {
         _credentials.Clear();
         IsAwaitingPasskeys = false;
      }

      /// <summary>
      /// Appends the next typed factor and compares the whole sequence so far.
      /// Like progressive login, a mistype poisons the in-dialog stack until
      /// <see cref="ClearCredentials"/> (Escape); later correct factors cannot recover it.
      /// </summary>
      public bool ValidateCredentials(string credential)
      {
         _credentials.Add(credential);
         IsAwaitingPasskeys = true;

         return _credentials.SequenceEqual(_realCredentials);
      }
   }
}
