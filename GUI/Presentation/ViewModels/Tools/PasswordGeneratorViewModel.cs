using System.Text;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools
{
   /// <summary>
   /// Password generator shared by hosts. Results that arrive after a newer
   /// request, or after <see cref="Close"/>, are dropped.
   /// </summary>
   internal sealed class PasswordGeneratorViewModel : ObservableObject
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = _title();

      public void OnLanguageChanged()
         => Title = _title();

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
            if (SetProperty(ref field, value ?? string.Empty))
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
            if (SetProperty(ref field, value ?? string.Empty))
            {
               GeneratePassword();
            }
         }
      } = string.Empty;

      public FieldVisualState PasswordState
         => FieldVisualStateHelper.ForEditedSecret(isDirty: false, isNotifiedWeak: SecretQuality.IsWeak(GeneratedPassword), isNotifiedLeak: _isLeaked);

      public static bool IsInsertAvailable => AppServices.Session.User is not null;

      public static int ClipboardAutoClearAfter
         => AppServices.Session.User?.Settings.CleaningClipboardTimeout ?? 0;

      public ICommand RegenerateCommand { get; }
      public ICommand CopyCommand { get; }
      public ICommand InsertCommand { get; }

      public event EventHandler? InsertRequested;

      public PasswordGeneratorViewModel()
      {
         Alphabet = _buildAlphabet();
         GeneratePassword();

         RegenerateCommand = new RelayCommand(GeneratePassword);
         CopyCommand = new RelayCommand(_copy);
         InsertCommand = new RelayCommand(() =>
         {
            _copy();
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
      private bool _closed;

      internal void GeneratePassword()
      {
         GeneratedPassword = string.Empty;

         if (!_closed)
         {
            _ = _generatePasswordAsync(Interlocked.Increment(ref _generation));
         }
      }

      /// <summary>Drops in-flight results and clears the generated secret when the view closes.</summary>
      public void Close()
      {
         _closed = true;
         _ = Interlocked.Increment(ref _generation);
         _ = Interlocked.Increment(ref _leakCheck);
         GeneratedPassword = string.Empty;
      }

      private void _copy()
      {
         if (!string.IsNullOrEmpty(GeneratedPassword))
         {
            AppServices.Clipboard?.SetText(GeneratedPassword, ClipboardAutoClearAfter);
         }
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
         catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or InvalidOperationException or ArgumentException or IOException)
         {
            // The exception text is not logged: it may echo generator input.
            AppServices.Log.Error($"Failed to generate a password ({ex.GetType().Name})");
         }
      }

      private void _scheduleLeakCheck()
      {
         _isLeaked = false;
         OnPropertyChanged(nameof(PasswordState));
         _ = _refreshLeakAsync(Interlocked.Increment(ref _leakCheck));
      }

      private async Task _refreshLeakAsync(int stamp)
      {
         if (_closed || !CheckIfLeaked || string.IsNullOrEmpty(GeneratedPassword))
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
               OnPropertyChanged(nameof(PasswordState));
            }
         }
         catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or InvalidOperationException or ArgumentException or IOException)
         {
            // Fail-open UI: leave the field unmarked rather than flash danger.
            AppServices.Log.Warn($"Leak check failed ({ex.GetType().Name})");
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

      private static string _title()
         => Strings.Format(nameof(Strings.Title_PasswordGenerator), AppServices.Info.Title);
   }
}
