using System.ComponentModel;
using System.Windows;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class VisiblePasswordBoxViewModel : INotifyPropertyChanged
   {
      private string _password = string.Empty;
      public string Password
      {
         get => _password;
         set
         {
            if (_password != value)
            {
               _password = value;
               OnPropertyChanged(nameof(Password));
            }
         }
      }

      private Visibility _passwordVisibility = Visibility.Visible;
      public Visibility PasswordVisibility
      {
         get => _passwordVisibility;
         set
         {
            if (_passwordVisibility != value)
            {
               _passwordVisibility = value;

               OnPropertyChanged(nameof(PasswordVisibility));
            }
         }
      }

      private Visibility _textVisibility = Visibility.Hidden;
      public Visibility TextVisibility
      {
         get => _textVisibility;
         set
         {
            if (_textVisibility != value)
            {
               _textVisibility = value;

               OnPropertyChanged(nameof(TextVisibility));
            }
         }
      }

      private Visibility _buttonVisibility = Visibility.Visible;
      public Visibility ButtonVisibility
      {
         get => _buttonVisibility;
         set
         {
            if (_buttonVisibility != value)
            {
               _buttonVisibility = value;

               OnPropertyChanged(nameof(ButtonVisibility));
            }
         }
      }

      public bool PasswordIsVisible => PasswordVisibility == System.Windows.Visibility.Hidden;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public void TooglePasswordVisibility()
      {
         if (!PasswordIsVisible)
         {
            PasswordVisibility = System.Windows.Visibility.Hidden;
            TextVisibility = System.Windows.Visibility.Visible;
         }
         else
         {
            PasswordVisibility = System.Windows.Visibility.Visible;
            TextVisibility = System.Windows.Visibility.Hidden;
         }
      }
   }
}
