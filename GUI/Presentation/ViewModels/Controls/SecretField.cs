using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Controls
{
   /// <summary>
   /// Hold-to-reveal state of a secret field: revealed while the eye button is held,
   /// masked again on release, when the app leaves the foreground and when the field
   /// is cleared. Hosts whose control can unmask itself (MAUI Entry) do not mirror;
   /// the others (WPF PasswordBox) show <see cref="RevealText"/>, which is only filled
   /// while revealed.
   /// </summary>
   /// <remarks>
   /// No command on purpose: reveal is press and release, which a command
   /// fired from the keyboard cannot express.
   /// </remarks>
   internal sealed class SecretField(ISecretInput input, bool mirrorsPlaintext) : ObservableObject
   {
      private readonly ISecretInput _input = input ?? throw new ArgumentNullException(nameof(input));

      public bool IsRevealed
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public string RevealText
      {
         get;
         private set => SetProperty(ref field, value);
      } = string.Empty;

      public bool IsEnabled
      {
         get;
         set => SetProperty(ref field, value);
      } = true;

      public void Reveal()
      {
         IsRevealed = true;
         RefreshMirror();
      }

      public void Mask()
      {
         IsRevealed = false;
         RevealText = string.Empty;
      }

      /// <summary>Call when the secret changes; re-reads it only while revealed.</summary>
      public void RefreshMirror()
      {
         if (IsRevealed && mirrorsPlaintext)
         {
            RevealText = _input.Read();
         }
      }

      public void Clear()
      {
         Mask();
         _input.Clear();
      }
   }
}
