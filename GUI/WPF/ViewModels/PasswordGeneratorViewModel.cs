using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class PasswordGeneratorViewModel : ObservableObject, ILanguageAware
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_PasswordGenerator), AppInfo.Title);

      public void OnLanguageChanged()
         => Title = Strings.Format(nameof(Strings.Title_PasswordGenerator), AppInfo.Title);

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
         set
         {
            if (SetProperty(ref field, value))
            {
               // Pasted or edited text must not call sync PasswordLeaked on the
               // UI thread; schedule an async check and paint from cached state.
               _scheduleLeakCheck();
            }
         }
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

      public Brush PasswordBackground
         => SecretFieldBrushes.Background(isDirty: false, isNotifiedWeak: SecretQuality.IsWeak(GeneratedPassword), isNotifiedLeak: _isLeaked);

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

      // Separate from _generation: paste/edit leak checks must not cancel an
      // in-flight regenerate, and a slow leak answer must not paint a newer paste.
      private int _leakCheck;
      private bool _isLeaked;

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

      private void _scheduleLeakCheck()
      {
         _isLeaked = false;
         OnPropertyChanged(nameof(PasswordBackground));
         _ = _refreshLeakAsync(Interlocked.Increment(ref _leakCheck));
      }

      private async Task _refreshLeakAsync(int stamp)
      {
         if (!CheckIfLeaked || string.IsNullOrEmpty(GeneratedPassword))
         {
            return;
         }

         string password = GeneratedPassword;

         try
         {
            bool leaked = await AppServices.PasswordFactory
               .PasswordLeakedAsync(password)
               .ConfigureAwait(true);

            if (stamp == Volatile.Read(ref _leakCheck))
            {
               _isLeaked = leaked;
               OnPropertyChanged(nameof(PasswordBackground));
            }
         }
         catch (OperationCanceledException)
         {
            // Fail-open UI: leave the field unmarked rather than flash danger.
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
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.UpperAlphabetic);
         }

         if (IncludeLowerCaseAlphabet)
         {
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.LowerAlphabetic);
         }

         if (IncludeSpecialCharacters)
         {
            _ = alphabetBuilder.Append(AppServices.PasswordFactory.SpecialChars);
         }

         return alphabetBuilder.ToString();
      }
   }
}
