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
            ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile => $"User {Activity.Data.ElementAt(0)}'s autosave merged and saved",
            ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile => $"User {Activity.Data.ElementAt(0)}'s autosave merged without saving",
            ActivityEventType.DontMergeAndRemoveAutoSaveFile => $"User {Activity.Data.ElementAt(0)}'s autosave not merged and removed",
            ActivityEventType.DontMergeAndKeepAutoSaveFile => $"User {Activity.Data.ElementAt(0)}'s autosave not merged and kept",
            ActivityEventType.DatabaseCreated => $"User {Activity.Data.ElementAt(0)}'s database created",
            ActivityEventType.DatabaseOpened => $"User {Activity.Data.ElementAt(0)}'s database opened",
            ActivityEventType.DatabaseSaved => $"User {Activity.Data.ElementAt(0)}'s database saved",
            ActivityEventType.DatabaseClosed => $"User {Activity.Data.ElementAt(0)}'s database closed",
            ActivityEventType.LoginSessionTimeoutReached => $"User {Activity.Data.ElementAt(0)}'s login session timeout reached",
            ActivityEventType.LoginFailed => $"User {Activity.Data.ElementAt(0)} login failed at level {Activity.Data.ElementAt(1)}",
            ActivityEventType.UserLoggedIn => $"User {Activity.Data.ElementAt(0)} logged in",
            ActivityEventType.UserLoggedOut => $"User {Activity.Data.ElementAt(0)} logged out {(!string.IsNullOrEmpty(Activity.Data.ElementAt(1)) ? "without saving" : "")}",
            ActivityEventType.ImportingDataStarted => $"Importing data from file : '{Activity.Data.ElementAt(0)}'",
            ActivityEventType.ImportingDataSucceded => $"Import completed successfully",
            ActivityEventType.ImportingDataFailed => $"Import failed because {Activity.Data.ElementAt(0)}",
            ActivityEventType.ExportingDataStarted => $"Exporting data to file : '{Activity.Data.ElementAt(0)}'",
            ActivityEventType.ExportingDataSucceded => $"Export completed successfully",
            ActivityEventType.ExportingDataFailed => $"Export failed because {Activity.Data.ElementAt(0)}",
#pragma warning disable CA1308 // Display text, not a normalization key: the field name is intentionally lowercased for a readable sentence.
            ActivityEventType.ItemUpdated => $"{(Activity.Data.Count() > 3 ? $"{Activity.Data.ElementAt(3)}'s " : "")}{Activity.Data.ElementAt(0)}'s {Activity.Data.ElementAt(1).ToSentenceCase().ToLowerInvariant()} has been {(string.IsNullOrWhiteSpace(Activity.Data.ElementAt(2)) ? $"updated" : $"set to {Activity.Data.ElementAt(2)}")}",
#pragma warning restore CA1308
            ActivityEventType.ItemAdded => $"{Activity.Data.ElementAt(2)} has been added to {Activity.Data.ElementAt(0)}",
            ActivityEventType.ItemDeleted => $"{Activity.Data.ElementAt(2)} has been removed from {Activity.Data.ElementAt(0)}",
            ActivityEventType.ActivityLogTampered => $"User {Activity.Data.ElementAt(0)}'s activity log integrity check failed",
            _ => $"{Activity}",
         };

         return message.Trim();
      }
   }
}
