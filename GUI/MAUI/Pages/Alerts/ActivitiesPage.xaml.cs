using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Pages.Alerts
{
   internal sealed partial class ActivitiesPage : ClosablePage
   {
      public ActivitiesPage(bool needsReview, string? searchCriteria)
      {
         InitializeComponent();
         UserActivitiesViewModel vm = new() { NeedsReview = needsReview };
         if (searchCriteria is not null)
         {
            vm.SearchCriteria = searchCriteria;
         }

         BindingContext = vm;
      }

      private void _onClose(object? sender, EventArgs e) => CompletePage();
   }
}
