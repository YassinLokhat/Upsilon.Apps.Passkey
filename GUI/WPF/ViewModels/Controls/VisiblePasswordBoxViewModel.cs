using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class VisiblePasswordBoxViewModel : INotifyPropertyChanged
   {
      /// <summary>
      /// Plaintext shown in the reveal <c>TextBox</c> only while the eye button
      /// is held. Cleared as soon as the password is masked again so the secret
      /// does not linger in the ViewModel.
      /// </summary>
      public string RevealText
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

      public event PropertyChangedEventHandler? PropertyChanged;

      public void ShowPassword()
      {
         PasswordVisibility = Visibility.Collapsed;
         TextVisibility = Visibility.Visible;
      }

      public void HidePassword()
      {
         PasswordVisibility = Visibility.Visible;
         TextVisibility = Visibility.Collapsed;
      }
   }
}
