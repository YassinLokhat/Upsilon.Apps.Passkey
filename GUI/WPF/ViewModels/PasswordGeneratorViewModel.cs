using System.Text;
using System.Windows;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Views;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class PasswordGeneratorViewModel : ObservableObject
   {
      public static string Title => AppInfo.Title + " - Password Generator";

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
         CopyCommand = new RelayCommand(() => QrCodeView.CopyToClipboard(GeneratedPassword));
         InsertCommand = new RelayCommand(() =>
         {
            QrCodeView.CopyToClipboard(GeneratedPassword);
            InsertRequested?.Invoke(this, EventArgs.Empty);
         });
      }

      internal void GeneratePassword()
      {
         GeneratedPassword = string.Empty;

         _ = Task.Run(() =>
         {
            GeneratedPassword = AppServices.PasswordFactory.GeneratePassword(PasswordLength, Alphabet, CheckIfLeaked);
         });
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
