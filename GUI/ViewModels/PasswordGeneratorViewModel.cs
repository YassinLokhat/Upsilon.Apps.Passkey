using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class PasswordGeneratorViewModel : INotifyPropertyChanged
   {
      public static string Title => MainViewModel.AppTitle + " - Password Generator";

      public bool CheckIfLeaked
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _includeCharactersChanged(nameof(CheckIfLeaked));
            }
         }
      } = true;

      public int PasswordLength
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(PasswordLength));
               GeneratePassword();
            }
         }
      } = 20;

      public string GeneratedPassword
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public bool IncludeNumerics
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _includeCharactersChanged(nameof(IncludeNumerics));
            }
         }
      } = true;

      public bool IncludeSpecialCharacters
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _includeCharactersChanged(nameof(IncludeSpecialCharacters));
            }
         }
      } = true;

      public bool IncludeLowerCaseAlphabet
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _includeCharactersChanged(nameof(IncludeLowerCaseAlphabet));
            }
         }
      } = true;

      public bool IncludeUpperCaseAlphabet
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _includeCharactersChanged(nameof(IncludeUpperCaseAlphabet));
            }
         }
      } = true;

      private string _alphabet;
      public string Alphabet
      {
         get => _alphabet;
         set
         {
            if (_alphabet != value)
            {
               _alphabet = value;
               OnPropertyChanged(nameof(Alphabet));
               GeneratePassword();
            }
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public PasswordGeneratorViewModel()
      {
         _alphabet = _buildAlphabet();
         GeneratePassword();
      }

      internal void GeneratePassword()
      {
         GeneratedPassword = string.Empty;

         _ = Task.Run(() =>
         {
            GeneratedPassword = MainViewModel.PasswordFactory.GeneratePassword(PasswordLength, Alphabet, CheckIfLeaked);
         });
      }

      private void _includeCharactersChanged(string propertyName)
      {
         OnPropertyChanged(propertyName);

         Alphabet = _buildAlphabet();
      }

      private string _buildAlphabet()
      {
         StringBuilder alphabetBuilder = new();

         if (IncludeNumerics)
         {
            _ = alphabetBuilder.Append(MainViewModel.PasswordFactory.Numeric);
         }

         if (IncludeUpperCaseAlphabet)
         {
            _ = alphabetBuilder.Append(MainViewModel.PasswordFactory.Alphabetic.ToUpper());
         }

         if (IncludeLowerCaseAlphabet)
         {
            _ = alphabetBuilder.Append(MainViewModel.PasswordFactory.Alphabetic.ToLower());
         }

         if (IncludeSpecialCharacters)
         {
            _ = alphabetBuilder.Append(MainViewModel.PasswordFactory.SpecialChars);
         }

         return alphabetBuilder.ToString();
      }
   }
}
