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
      public string Message => _buildMessage(Activity);
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
               || Message.Contains(searchCriteria, StringComparison.OrdinalIgnoreCase);

         return fromDateMatches
            && toDateMatches
            && eventTypeMatches
            && needsReviewMatches
            && searchCriteriaMatches;
      }

      private static string _buildMessage(IActivity activity)
      {
         string message = activity.EventType switch
         {
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => Strings.Format(nameof(Strings.Activity_MergeAndSaveThenRemoveAutoSaveFile), activity.Username),
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => Strings.Format(nameof(Strings.Activity_MergeWithoutSavingAndKeepAutoSaveFile), activity.Username),
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => Strings.Format(nameof(Strings.Activity_DontMergeAndRemoveAutoSaveFile), activity.Username),
            ActivityEventType.DontMergeAndKeepAutoSaveFile => Strings.Format(nameof(Strings.Activity_DontMergeAndKeepAutoSaveFile), activity.Username),
            ActivityEventType.DatabaseCreated => Strings.Format(nameof(Strings.Activity_DatabaseCreated), activity.Username),
            ActivityEventType.DatabaseOpened => Strings.Format(nameof(Strings.Activity_DatabaseOpened), activity.Username),
            ActivityEventType.DatabaseSaved => Strings.Format(nameof(Strings.Activity_DatabaseSaved), activity.Username),
            ActivityEventType.DatabaseClosed => Strings.Format(nameof(Strings.Activity_DatabaseClosed), activity.Username),
            ActivityEventType.LoginSessionTimeoutReached => Strings.Format(nameof(Strings.Activity_LoginSessionTimeoutReached), activity.Username),
            ActivityEventType.LoginFailed => Strings.Format(nameof(Strings.Activity_LoginFailed), activity.Username, activity.FieldValue),
            ActivityEventType.UserLoggedIn => Strings.Format(nameof(Strings.Activity_UserLoggedIn), activity.Username),
            ActivityEventType.UserLoggedOut => StringsHelper.ComputeUserLoggedOutStrings(activity),
            ActivityEventType.ImportingDataStarted => Strings.Format(nameof(Strings.Activity_ImportingDataStarted), activity.FieldValue),
            ActivityEventType.ImportingDataSucceded => Strings.Activity_ImportingDataSucceded,
            ActivityEventType.ImportingDataFailed => Strings.Format(nameof(Strings.Activity_ImportingDataFailed), activity.FieldValue),
            ActivityEventType.ExportingDataStarted => Strings.Format(nameof(Strings.Activity_ExportingDataStarted), activity.FieldValue),
            ActivityEventType.ExportingDataSucceded => Strings.Activity_ExportingDataSucceded,
            ActivityEventType.ExportingDataFailed => Strings.Format(nameof(Strings.Activity_ExportingDataFailed), activity.FieldValue),
            ActivityEventType.ItemUpdated => StringsHelper.ComputeItemUpdatedStrings(activity),
            ActivityEventType.ItemAdded => StringsHelper.ComputeItemAddedStrings(activity),
            ActivityEventType.ItemDeleted => StringsHelper.ComputeItemItemDeletedStrings(activity),
            ActivityEventType.ActivityLogTampered => Strings.Format(nameof(Strings.Activity_ActivityLogTampered), activity.Username),
            _ => $"{activity}",
         };

         return TextHelper.ToSentenceCase(message.Trim());
      }
   }
}
