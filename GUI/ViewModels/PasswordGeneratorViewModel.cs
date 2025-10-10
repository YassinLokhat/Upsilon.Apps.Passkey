using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class PasswordGeneratorViewModel : INotifyPropertyChanged
   {
      public string PasswordGeneratorWindowTitle => MainViewModel.AppTitle + " - Password Generator";

      private Visibility _allowInsert = Visibility.Hidden;
      public Visibility AllowInsert
      {
         get => _allowInsert;
         set
         {
            if (_allowInsert != value)
            {
               _allowInsert = value;
               _includeCharactersChanged(nameof(AllowInsert));
            }
         }
      }

      private bool _checkIfLeaked = true;
      public bool CheckIfLeaked
      {
         get => _checkIfLeaked;
         set
         {
            if (_checkIfLeaked != value)
            {
               _checkIfLeaked = value;
               _includeCharactersChanged(nameof(CheckIfLeaked));
            }
         }
      }

      private int _passwordLength = 20;
      public int PasswordLength
      {
         get => _passwordLength;
         set
         {
            if (_passwordLength != value)
            {
               _passwordLength = value;
               OnPropertyChanged(nameof(PasswordLength));
            }
         }
      }

      private string _generatedPassword = string.Empty;
      public string GeneratedPassword
      {
         get => _generatedPassword;
         set
         {
            if (_generatedPassword != value)
            {
               _generatedPassword = value;
               OnPropertyChanged(nameof(GeneratedPassword));
            }
         }
      }

      private bool _includeNumerics = true;
      public bool IncludeNumerics
      {
         get => _includeNumerics;
         set
         {
            if (_includeNumerics != value)
            {
               _includeNumerics = value;
               _includeCharactersChanged(nameof(IncludeNumerics));
            }
         }
      }

      private bool _includeSpecialCharacters = true;
      public bool IncludeSpecialCharacters
      {
         get => _includeSpecialCharacters;
         set
         {
            if (_includeSpecialCharacters != value)
            {
               _includeSpecialCharacters = value;
               _includeCharactersChanged(nameof(IncludeSpecialCharacters));
            }
         }
      }

      private bool _includeLowerCaseAlphabet = true;
      public bool IncludeLowerCaseAlphabet
      {
         get => _includeLowerCaseAlphabet;
         set
         {
            if (_includeLowerCaseAlphabet != value)
            {
               _includeLowerCaseAlphabet = value;
               _includeCharactersChanged(nameof(IncludeLowerCaseAlphabet));
            }
         }
      }

      private bool _includeUpperCaseAlphabet = true;
      public bool IncludeUpperCaseAlphabet
      {
         get => _includeUpperCaseAlphabet;
         set
         {
            if (_includeUpperCaseAlphabet != value)
            {
               _includeUpperCaseAlphabet = value;
               _includeCharactersChanged(nameof(IncludeUpperCaseAlphabet));
            }
         }
      }

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

      private PasswordFactory _passwordFactory;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public PasswordGeneratorViewModel(PasswordFactory passwordFactory)
      {
         _passwordFactory = passwordFactory;

         _alphabet = _buildAlphabet();
         GeneratePassword();
      }

      internal void GeneratePassword()
      {
         GeneratedPassword = _passwordFactory.GeneratePassword(PasswordLength, Alphabet, CheckIfLeaked);
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
            alphabetBuilder.Append(_passwordFactory.Numeric);
         }

         if (IncludeUpperCaseAlphabet)
         {
            alphabetBuilder.Append(_passwordFactory.Alphabetic.ToUpper());
         }

         if (IncludeLowerCaseAlphabet)
         {
            alphabetBuilder.Append(_passwordFactory.Alphabetic.ToLower());
         }

         if (IncludeSpecialCharacters)
         {
            alphabetBuilder.Append(_passwordFactory.SpecialChars);
         }

         return alphabetBuilder.ToString();
      }
   }
}
