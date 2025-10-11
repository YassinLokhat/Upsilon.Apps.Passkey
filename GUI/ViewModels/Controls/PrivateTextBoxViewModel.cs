using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.Views.Controls;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class PrivateTextBoxViewModel : INotifyPropertyChanged
   {
      private string _privateText = string.Empty;
      public string PrivateText => _privateText;

      private char _passwordChar = '\0';
      public char PasswordChar => _passwordChar;

      private string _displayText = "******************";
      public string DisplayText
      {
         get => _displayText;
         set
         {
            if (_displayText != value)
            {
               _displayText = value;
               OnPropertyChanged(nameof(DisplayText));
            }
         }
      }

      //private readonly string _shownText = "👓";
      //private readonly string _hidenText = "👁";

      public PrivateTextBox.PrivateTextBoxOffFocusDisplayBehavior OffFocusDisplayBehavior { get; set; } = PrivateTextBox.PrivateTextBoxOffFocusDisplayBehavior.Classic;

      public PrivateTextBox.PrivateTextBoxShowPrivateTextBehavior ShowPrivateTextBehavior { get; set; } = PrivateTextBox.PrivateTextBoxShowPrivateTextBehavior.ShowOnFocus;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
