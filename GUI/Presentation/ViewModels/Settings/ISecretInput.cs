namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>
   /// Host control that holds a secret while it is edited (WPF PasswordBox, MAUI
   /// Entry), so the ViewModel never keeps a bindable plaintext copy.
   /// </summary>
   internal interface ISecretInput
   {
      string Read();

      void Write(string secret);

      void Clear();
   }
}
