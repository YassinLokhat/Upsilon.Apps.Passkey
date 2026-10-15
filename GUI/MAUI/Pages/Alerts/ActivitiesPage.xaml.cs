using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Alerts
{
   internal sealed partial class ActivitiesPage : ClosablePage
   {
      public ActivitiesPage(bool needsReview)
      {
         InitializeComponent();
         BindingContext = new UserActivitiesViewModel { NeedsReview = needsReview };
      }

      private void _onClose(object? sender, EventArgs e) => CompletePage();
   }
}
