using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Alerts
{
   internal sealed partial class DuplicatedPasswordsAlertPage : ClosablePage
   {
      public DuplicatedPasswordsAlertPage()
      {
         InitializeComponent();
         BindingContext = new DuplicatedPasswordsAlertViewModel();
      }

      private void _onClose(object? sender, EventArgs e) => CompletePage();
   }
}
