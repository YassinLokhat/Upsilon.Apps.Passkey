using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Alerts
{
   internal sealed partial class AccountPasswordsAlertPage : ClosablePage
   {
      public AccountPasswordsAlertPage(string kind)
      {
         InitializeComponent();
         BindingContext = new AccountPasswordsAlertViewModel { Kind = kind };
      }

      private void _onClose(object? sender, EventArgs e) => CompletePage();
   }
}
