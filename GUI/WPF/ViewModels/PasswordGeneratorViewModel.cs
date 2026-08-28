using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class PasswordGeneratorViewModel : ObservableObject, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_PasswordGenerator), AppInfo.Title);

      public void OnLanguageChanged()
         => OnPropertyChanged(nameof(Title));

      public bool CheckIfLeaked
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _includeCharactersChanged();
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
         set => SetProperty(ref field, value);
      } = string.Empty;

      public bool IncludeNumerics
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _includeCharactersChanged();
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
               _includeCharactersChanged();
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
               _includeCharactersChanged();
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
               _includeCharactersChanged();
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

      public static Visibility InsertVisibility => AppServices.Session.User is not null ? Visibility.Visible : Visibility.Collapsed;

      public ICommand RegenerateCommand { get; }
      public ICommand CopyCommand { get; }
      public ICommand InsertCommand { get; }

      public event EventHandler? InsertRequested;

      public PasswordGeneratorViewModel()
      {
         Alphabet = _buildAlphabet();
         GeneratePassword();

         RegenerateCommand = new RelayCommand(GeneratePassword);
         CopyCommand = new RelayCommand(() => AppServices.Clipboard.SetText(GeneratedPassword, ClipboardManager.AutoClearAfter));
         InsertCommand = new RelayCommand(() =>
         {
            AppServices.Clipboard.SetText(GeneratedPassword, ClipboardManager.AutoClearAfter);
            InsertRequested?.Invoke(this, EventArgs.Empty);
         });
      }

      // Every option change restarts a generation, and each one may wait on the
      // leak-check service. Stamping the requests lets a slow answer be dropped
      // instead of overwriting the result of the request that came after it.
      private int _generation;

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

            // Awaiting with the UI context captured means this assignment - and
            // the binding update it raises - happens back on the UI thread.
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

      private void _includeCharactersChanged()
      {
         Alphabet = _buildAlphabet();
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
#pragma warning disable CA1308 // Not a normalization key: lower-case letters are a legitimate part of the password character set.
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
