using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Settings
{
   internal sealed partial class PasskeyRowView : ContentView, ISecretInput
   {
      private readonly EntrySecretInput _secret;
      private PasskeyEntryViewModel? _entry;

      public PasskeyRowView()
      {
         InitializeComponent();
         _secret = new EntrySecretInput(this, SecretEntry);
      }

      public string Read() => _secret.Read();

      public void Write(string secret) => _secret.Write(secret);

      public void Clear() => _secret.Clear();

      protected override void OnBindingContextChanged()
      {
         base.OnBindingContextChanged();
         if (ReferenceEquals(_entry, BindingContext))
         {
            return;
         }

         _entry = BindingContext as PasskeyEntryViewModel;
         _entry?.Attach(this);
      }

      private void _onRevealPressed(object? sender, EventArgs e) => _secret.Field.Reveal();

      private void _onRevealReleased(object? sender, EventArgs e) => _secret.Field.Mask();
   }
}
