using System.ComponentModel;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class VisiblePasswordBoxViewModel : INotifyPropertyChanged
   {
      private string _password = string.Empty;
      public string Password
      {
         get => _password;
         set => PropertyHelper.SetProperty(ref _password, value, this, PropertyChanged);
      }

      private Visibility _passwordVisibility = Visibility.Visible;
      public Visibility PasswordVisibility
      {
         get => _passwordVisibility;
         set => PropertyHelper.SetProperty(ref _passwordVisibility, value, this, PropertyChanged);
      }

      private Visibility _textVisibility = Visibility.Hidden;
      public Visibility TextVisibility
      {
         get => _textVisibility;
         set => PropertyHelper.SetProperty(ref _textVisibility, value, this, PropertyChanged);
      }

      private Visibility _buttonVisibility = Visibility.Visible;
      public Visibility ButtonVisibility
      {
         get => _buttonVisibility;
         set => PropertyHelper.SetProperty(ref _buttonVisibility, value, this, PropertyChanged);
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
