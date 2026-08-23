using System.ComponentModel;
using Upsilon.Apps.Passkey.Core.Utils;
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
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => $"User {activity.ItemName}'s autosave merged and saved",
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => $"User {activity.ItemName}'s autosave merged without saving",
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => $"User {activity.ItemName}'s autosave not merged and removed",
            ActivityEventType.DontMergeAndKeepAutoSaveFile => $"User {activity.ItemName}'s autosave not merged and kept",
            ActivityEventType.DatabaseCreated => $"User {activity.ItemName}'s database created",
            ActivityEventType.DatabaseOpened => $"User {activity.ItemName}'s database opened",
            ActivityEventType.DatabaseSaved => $"User {activity.ItemName}'s database saved",
            ActivityEventType.DatabaseClosed => $"User {activity.ItemName}'s database closed",
            ActivityEventType.LoginSessionTimeoutReached => $"User {activity.ItemName}'s login session timeout reached",
            ActivityEventType.LoginFailed => $"User {activity.ItemName} login failed at level {activity.FieldValue}",
            ActivityEventType.UserLoggedIn => $"User {activity.ItemName} logged in",
            ActivityEventType.UserLoggedOut => $"User {activity.ItemName} logged out {(!string.IsNullOrEmpty(activity.FieldValue) ? "without saving" : "")}",
            ActivityEventType.ImportingDataStarted => $"Importing data from file : '{activity.FieldValue}'",
            ActivityEventType.ImportingDataSucceded => $"Import completed successfully",
            ActivityEventType.ImportingDataFailed => $"Import failed because {activity.FieldValue}",
            ActivityEventType.ExportingDataStarted => $"Exporting data to file : '{activity.FieldValue}'",
            ActivityEventType.ExportingDataSucceded => $"Export completed successfully",
            ActivityEventType.ExportingDataFailed => $"Export failed because {activity.FieldValue}",
#pragma warning disable CA1308 // Display text, not a normalization key: the field name is intentionally lowercased for a readable sentence.
            ActivityEventType.ItemUpdated => $"{(!string.IsNullOrEmpty(activity.ParentName) ? $"{activity.ParentName}'s " : "")}{activity.ItemName}'s {activity.FieldName?.ToSentenceCase().ToLowerInvariant()} has been {(string.IsNullOrWhiteSpace(activity.FieldValue) ? $"updated" : $"set to {activity.FieldValue}")}",
#pragma warning restore CA1308
            ActivityEventType.ItemAdded => $"{activity.FieldValue} has been added to {activity.ItemName}",
            ActivityEventType.ItemDeleted => $"{activity.FieldValue} has been removed from {activity.ItemName}",
            ActivityEventType.ActivityLogTampered => $"User {activity.ItemName}'s activity log integrity check failed",
            _ => $"{activity}",
         };

         return message.Trim();
      }
   }
}
