using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal sealed class ActivityViewModel(IActivity activity) : INotifyPropertyChanged
   {
      public readonly IActivity Activity = activity;
      public string DateTime => Activity.DateTime.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
      public string EventType => Activity.EventType.ToReadableString();
      public string Message => Activity.Message;
      public bool NeedsReview
      {
         get => Activity.NeedsReview;
         set
         {
            if (Activity.NeedsReview != value)
            {
               Activity.NeedsReview = value;
               _onPropertyChanged(nameof(NeedsReview));
               _onPropertyChanged(nameof(NeedsReviewString));
            }
         }
      }
      public string NeedsReviewString => NeedsReview ? Strings.Label_NeedsReviewValue : Strings.Label_Reviewed;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public bool MeetsConditions(DateTime fromDateFilter, DateTime toDateFilter, ActivityEventType eventType, string searchCriteria, bool needsReview)
      {
         bool fromDateMatches = fromDateFilter > System.DateTime.Now.Date || Activity.DateTime.Date >= fromDateFilter;
         bool toDateMatches = toDateFilter > System.DateTime.Now.Date || Activity.DateTime.Date <= toDateFilter;
         bool eventTypeMatches = eventType == ActivityEventType.None || Activity.EventType == eventType;
         bool needsReviewMatches = !needsReview || Activity.NeedsReview;

         bool itemIdMatches = string.IsNullOrEmpty(Activity.ItemId)
                  && !string.IsNullOrEmpty(AppServices.Session.User?.ItemId)
                  && searchCriteria == AppServices.Session.User?.ItemId;

         bool searchCriteriaMatches = itemIdMatches
               || Activity.ItemId == searchCriteria
               || Activity.Message.Contains(searchCriteria, StringComparison.OrdinalIgnoreCase);

         return fromDateMatches
            && toDateMatches
            && eventTypeMatches
            && needsReviewMatches
            && searchCriteriaMatches;
      }
   }
}
