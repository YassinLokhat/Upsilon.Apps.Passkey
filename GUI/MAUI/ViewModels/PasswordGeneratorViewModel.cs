using System.Text;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class PasswordGeneratorViewModel : ObservableObject
   {
      private int _generation;

      public PasswordGeneratorViewModel()
      {
         Alphabet = _buildAlphabet();
         GeneratePassword();
         RegenerateCommand = new RelayCommand(GeneratePassword);
         CopyCommand = new RelayCommand(() =>
         {
            if (!string.IsNullOrEmpty(GeneratedPassword))
            {
               AppServices.Clipboard.SetText(GeneratedPassword, ClipboardManager.AutoClearAfter);
               StatusMessage = Strings.Msg_Copied;
            }
         });
         ApplyToAccountCommand = new RelayCommand(_applyToAccount, () => ServicesViewModel.CanApplyPassword && !string.IsNullOrEmpty(GeneratedPassword));
         BackCommand = new AsyncRelayCommand(async () =>
         {
            ServicesViewModel.ClearPasswordApplyTarget();
            await AppServices.Navigation.GoBackAsync().ConfigureAwait(true);
         });
      }

      public string Title => Strings.Format(nameof(Strings.Title_PasswordGenerator), PasskeyAppInfo.Title);

      public bool CheckIfLeaked
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Alphabet = _buildAlphabet();
            }
         }
      } = true;

      public int PasswordLength
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               GeneratePassword();
            }
         }
      } = 20;

      public string GeneratedPassword
      {
         get;
         set
         {
            if (SetProperty(ref field, value) && ApplyToAccountCommand is RelayCommand apply)
            {
               apply.NotifyCanExecuteChanged();
            }
         }
      } = string.Empty;

      public string StatusMessage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public bool IncludeNumerics
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Alphabet = _buildAlphabet();
            }
         }
      } = true;

      public bool IncludeSpecialCharacters
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Alphabet = _buildAlphabet();
            }
         }
      } = true;

      public bool IncludeLowerCaseAlphabet
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Alphabet = _buildAlphabet();
            }
         }
      } = true;

      public bool IncludeUpperCaseAlphabet
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Alphabet = _buildAlphabet();
            }
         }
      } = true;

      public string Alphabet
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               GeneratePassword();
            }
         }
      } = string.Empty;

      public ICommand RegenerateCommand { get; }
      public ICommand CopyCommand { get; }
      public ICommand ApplyToAccountCommand { get; }
      public ICommand BackCommand { get; }

      public bool CanApplyToAccount => ServicesViewModel.CanApplyPassword;

      private void _applyToAccount()
      {
         if (ServicesViewModel.TryApplyGeneratedPassword(GeneratedPassword))
         {
            StatusMessage = Strings.Msg_Saved;
         }
      }

      internal void GeneratePassword()
      {
         GeneratedPassword = string.Empty;
         _ = _generatePasswordAsync(Interlocked.Increment(ref _generation));
      }

      private async Task _generatePasswordAsync(int generation)
      {
         try
         {
            string password = await AppServices.PasswordFactory
               .GeneratePasswordAsync(PasswordLength, Alphabet, CheckIfLeaked)
               .ConfigureAwait(true);

            if (generation == Volatile.Read(ref _generation))
            {
               GeneratedPassword = password;
            }
         }
         catch (OperationCanceledException ex)
         {
            Log.Error(ex, "Failed to generate a password");
         }
      }

      private string _buildAlphabet()
      {
         StringBuilder alphabetBuilder = new();

         if (IncludeNumerics)
         {
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.Numeric);
         }

         if (IncludeUpperCaseAlphabet)
         {
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.Alphabetic.ToUpperInvariant());
         }

         if (IncludeLowerCaseAlphabet)
         {
#pragma warning disable CA1308
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.Alphabetic.ToLowerInvariant());
#pragma warning restore CA1308
         }

         if (IncludeSpecialCharacters)
         {
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.SpecialChars);
         }

         return alphabetBuilder.ToString();
      }
   }
}
