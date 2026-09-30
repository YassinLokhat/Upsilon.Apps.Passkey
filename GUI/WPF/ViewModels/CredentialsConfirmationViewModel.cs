using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class CredentialsConfirmationViewModel(IEnumerable<string> credentials, bool isNew) : ObservableObject, ILanguageAware
   {
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

      private readonly IEnumerable<string> _realCredentials = credentials;

      private readonly List<string> _credentials = [];

      public void OnLanguageChanged()
      {
         OnPropertyChanged(nameof(Title));
         OnPropertyChanged(nameof(CredentialsLabel));
      }

      public void ClearCredentials()
      {
         _credentials.Clear();
         IsAwaitingPasskeys = false;
      }

      public bool ValidateCredentials(string credential)
      {
         _credentials.Add(credential);
         IsAwaitingPasskeys = true;

         return _credentials.SequenceEqual(_realCredentials);
      }
   }
}
