using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls
{
   internal class ActivityViewModel(IActivity activity) : INotifyPropertyChanged
   {
      public readonly IActivity Activity = activity;
      public string DateTime => Activity.DateTime.ToString("yyyy-MM-dd HH:mm");
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
               OnPropertyChanged(nameof(NeedsReview));
               OnPropertyChanged(nameof(NeedsReviewString));
            }
         }
      }
      public string NeedsReviewString => NeedsReview ? "Needs review" : "Reviewed";

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public bool MeetsConditions(string itemId, DateTime fromDateFilter, DateTime toDateFilter, ActivityEventType eventType, string message, bool needsReview)
      {
         return (string.IsNullOrEmpty(itemId) || Activity.ItemId == itemId)
            && (fromDateFilter > System.DateTime.Now.Date
               || Activity.DateTime.Date >= fromDateFilter) && (toDateFilter > System.DateTime.Now.Date
               || Activity.DateTime.Date <= toDateFilter) && (eventType == ActivityEventType.None
               || Activity.EventType == eventType) && (!needsReview
               || Activity.NeedsReview) && Activity.Message.Contains(message, StringComparison.CurrentCultureIgnoreCase);
      }
   }
}