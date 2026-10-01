using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   /// <summary>
   /// Progressive credential re-entry for create / update / delete / export.
   /// Mistakes intentionally poison the in-dialog sequence until Escape resets it
   /// (same no-rollback rule as progressive login — not a UX defect).
   /// </summary>
   internal class CredentialsConfirmationViewModel(IEnumerable<string> credentials, bool isNew) : ObservableObject, ILanguageAware
   {
      private readonly bool _isNew = isNew;
      private string[]? _expected = [.. credentials];
      private int _acceptedCount;

      /// <summary>
      /// Set on the first mismatched factor. Deliberate: further correct factors
      /// cannot recover until <see cref="ClearCredentials"/> (Escape), mirroring
      /// progressive login's no-rollback onion stack.
      /// </summary>
      private bool _poisoned;

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = isNew ? Strings.Title_NewCredentialsConfirmation : Strings.Title_ActualCredentialsConfirmation;

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
         Title = _isNew ? Strings.Title_NewCredentialsConfirmation : Strings.Title_ActualCredentialsConfirmation;
         OnPropertyChanged(nameof(CredentialsLabel));
      }

      /// <summary>
      /// Clears the poison flag and accepted count so the user can restart the
      /// full sequence. Bound to Escape in the view — not a cancel of the dialog.
      /// </summary>
      public void ClearCredentials()
      {
         _acceptedCount = 0;
         _poisoned = false;
         IsAwaitingPasskeys = false;
      }

      /// <summary>
      /// Drops references to expected credential strings after the dialog closes.
      /// </summary>
      public void ReleaseExpectedCredentials()
      {
         if (_expected is null)
         {
            return;
         }

         Array.Clear(_expected);
         _expected = null;
         ClearCredentials();
      }

      /// <summary>
      /// Accepts the next typed factor against the expected sequence.
      /// A mismatch intentionally poisons the dialog until
      /// <see cref="ClearCredentials"/> (Escape); later correct factors cannot
      /// recover it. Same progressive no-rollback rule as login — not a bug.
      /// Typed factors are not retained — only the accepted count / poison flag.
      /// </summary>
      public bool ValidateCredentials(string credential)
      {
         string[]? expected = _expected;
         if (expected is null || _poisoned)
         {
            IsAwaitingPasskeys = true;
            return false;
         }

         int index = _acceptedCount;
         if (index >= expected.Length
            || !string.Equals(credential, expected[index], StringComparison.Ordinal))
         {
            // Intentional poison (login parity): do not pop or retry this step quietly.
            _poisoned = true;
            IsAwaitingPasskeys = true;
            return false;
         }

         _acceptedCount++;
         IsAwaitingPasskeys = true;
         return _acceptedCount == expected.Length;
      }
   }
}
