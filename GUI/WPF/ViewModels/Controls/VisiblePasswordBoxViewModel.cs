using Upsilon.Apps.Passkey.GUI.WPF.Helper;

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

      public bool IsRevealed
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public bool CanReveal
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public bool IsEnabled
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public void ShowPassword() => IsRevealed = true;

      public void HidePassword() => IsRevealed = false;
   }
}
