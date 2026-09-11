using System.Windows;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class VisiblePasswordBoxViewModel : ObservableObject
   {
      /// <summary>
      /// Plaintext shown in the reveal <c>TextBox</c> only while the eye button
      /// is held. Cleared as soon as the password is masked again so the secret
      /// does not linger in the ViewModel.
      /// </summary>
      public string RevealText
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public Visibility PasswordVisibility
      {
         get;
         set => SetProperty(ref field, value);
      } = Visibility.Visible;

      public Visibility TextVisibility
      {
         get;
         set => SetProperty(ref field, value);
      } = Visibility.Collapsed;

      public Visibility ButtonVisibility
      {
         get;
         set => SetProperty(ref field, value);
      } = Visibility.Visible;

      public bool IsEnabled
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public Brush Background
      {
         get;
         set => SetProperty(ref field, value);
      } = DarkMode.UnchangedBrush2;

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
