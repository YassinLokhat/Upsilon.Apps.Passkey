using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   /// <summary>
   /// Secret held by a masked <see cref="Entry"/>. <see cref="Field"/> unmasks the Entry
   /// itself while revealed (no plaintext mirror) and masks it again when the app
   /// leaves the foreground.
   /// </summary>
   internal sealed class EntrySecretInput : ISecretInput
   {
      private readonly Entry _entry;

      public EntrySecretInput(VisualElement owner, Entry entry)
      {
         ArgumentNullException.ThrowIfNull(entry);
         _entry = entry;
         _entry.IsPassword = true;

         Field = new SecretField(this, mirrorsPlaintext: false);
         Field.PropertyChanged += (_, e) =>
         {
            if (e.PropertyName == nameof(SecretField.IsRevealed))
            {
               _entry.IsPassword = !Field.IsRevealed;
            }
         };
         BackgroundGuard.Attach(owner, Field.Mask);
      }

      public SecretField Field { get; }

      public string Read() => _entry.Text ?? string.Empty;

      public void Write(string secret) => _entry.Text = secret;

      public void Clear()
      {
         Field.Mask();
         _entry.Text = string.Empty;
      }
   }
}
