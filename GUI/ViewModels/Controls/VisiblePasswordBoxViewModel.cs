using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;

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
      } = Visibility.Collapsed;
      public Visibility ButtonVisibility
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = Visibility.Visible;

      public bool IsEnabled
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = true;

      public Brush Background
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = DarkMode.UnchangedBrush2;

      public bool PasswordIsVisible => PasswordVisibility == System.Windows.Visibility.Collapsed;

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public void TooglePasswordVisibility()
      {
         if (!PasswordIsVisible)
         {
            PasswordVisibility = System.Windows.Visibility.Collapsed;
            TextVisibility = System.Windows.Visibility.Visible;
         }
         else
         {
            PasswordVisibility = System.Windows.Visibility.Visible;
            TextVisibility = System.Windows.Visibility.Collapsed;
         }
      }
   }
}
