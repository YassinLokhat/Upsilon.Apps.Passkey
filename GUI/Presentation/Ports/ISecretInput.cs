namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>
   /// Host control that holds a secret while it is typed or edited (WPF PasswordBox,
   /// MAUI Entry), so a ViewModel never keeps a bindable plaintext copy.
   /// </summary>
   internal interface ISecretInput
   {
      /// <summary>Plaintext for one immediate use; never store the result.</summary>
      string Read();

      void Write(string secret);

      void Clear();
   }
}
