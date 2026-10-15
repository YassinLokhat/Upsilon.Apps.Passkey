using Upsilon.Apps.Passkey.GUI.MAUI.Adapters;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Settings
{
   internal sealed partial class PasskeyRowView : ContentView, ISecretInput
   {
      private PasskeyEntryViewModel? _entry;

      public PasskeyRowView()
      {
         InitializeComponent();
         BackgroundGuard.Attach(this, () => SecretEntry.IsPassword = true);
      }

      public string Read() => SecretEntry.Text ?? string.Empty;

      public void Write(string secret) => SecretEntry.Text = secret;

      public void Clear()
      {
         SecretEntry.IsPassword = true;
         SecretEntry.Text = string.Empty;
      }

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

      private void _onRevealPressed(object? sender, EventArgs e) => SecretEntry.IsPassword = false;

      private void _onRevealReleased(object? sender, EventArgs e) => SecretEntry.IsPassword = true;
   }
}
