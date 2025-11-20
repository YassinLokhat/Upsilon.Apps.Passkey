using System.ComponentModel;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.Helper;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   public class VisiblePasswordBoxViewModel : INotifyPropertyChanged
   {
      public string Password
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;
      public Visibility PasswordVisibility
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = Visibility.Visible;
      public Visibility TextVisibility
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = Visibility.Hidden;
      public Visibility ButtonVisibility
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = Visibility.Visible;

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
