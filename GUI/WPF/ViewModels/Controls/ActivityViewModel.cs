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
      public string Message => _buildMessage();
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

      private string _buildMessage()
      {
         string message = Activity.EventType switch
         {
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => $"User {Activity.ItemName}'s autosave merged and saved",
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => $"User {Activity.ItemName}'s autosave merged without saving",
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => $"User {Activity.ItemName}'s autosave not merged and removed",
            ActivityEventType.DontMergeAndKeepAutoSaveFile => $"User {Activity.ItemName}'s autosave not merged and kept",
            ActivityEventType.DatabaseCreated => $"User {Activity.ItemName}'s database created",
            ActivityEventType.DatabaseOpened => $"User {Activity.ItemName}'s database opened",
            ActivityEventType.DatabaseSaved => $"User {Activity.ItemName}'s database saved",
            ActivityEventType.DatabaseClosed => $"User {Activity.ItemName}'s database closed",
            ActivityEventType.LoginSessionTimeoutReached => $"User {Activity.ItemName}'s login session timeout reached",
            ActivityEventType.LoginFailed => $"User {Activity.ItemName} login failed at level {Activity.FieldValue}",
            ActivityEventType.UserLoggedIn => $"User {Activity.ItemName} logged in",
            ActivityEventType.UserLoggedOut => $"User {Activity.ItemName} logged out {(!string.IsNullOrEmpty(Activity.FieldValue) ? "without saving" : "")}",
            ActivityEventType.ImportingDataStarted => $"Importing data from file : '{Activity.FieldValue}'",
            ActivityEventType.ImportingDataSucceded => $"Import completed successfully",
            ActivityEventType.ImportingDataFailed => $"Import failed because {Activity.FieldValue}",
            ActivityEventType.ExportingDataStarted => $"Exporting data to file : '{Activity.FieldValue}'",
            ActivityEventType.ExportingDataSucceded => $"Export completed successfully",
            ActivityEventType.ExportingDataFailed => $"Export failed because {Activity.FieldValue}",
#pragma warning disable CA1308 // Display text, not a normalization key: the field name is intentionally lowercased for a readable sentence.
            ActivityEventType.ItemUpdated => $"{(!string.IsNullOrEmpty(Activity.ParentName) ? $"{Activity.ParentName}'s " : "")}{Activity.ItemName}'s {Activity.FieldName?.ToSentenceCase().ToLowerInvariant()} has been {(string.IsNullOrWhiteSpace(Activity.FieldValue) ? $"updated" : $"set to {Activity.FieldValue}")}",
#pragma warning restore CA1308
            ActivityEventType.ItemAdded => $"{Activity.ItemName} has been added to {Activity.ParentName}",
            ActivityEventType.ItemDeleted => $"{Activity.ItemName} has been removed from {Activity.ParentName}",
            ActivityEventType.ActivityLogTampered => $"User {Activity.ItemName}'s activity log integrity check failed",
            _ => $"{Activity}",
         };

         return message.Trim();
      }
   }
}
