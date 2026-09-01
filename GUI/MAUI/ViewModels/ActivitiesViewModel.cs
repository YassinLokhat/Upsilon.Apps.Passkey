using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class ActivityRowViewModel : ObservableObject
   {
      public ActivityRowViewModel(IActivity activity)
      {
         Activity = activity;
         DateTimeText = activity.DateTime.ToString(
            Strings.Activity_DateTimeFormat,
            CultureInfo.InvariantCulture);
         EventTypeText = activity.EventType.ToReadableString();
         Message = _buildMessage(activity);
         NeedsReview = activity.NeedsReview;
      }

      public IActivity Activity { get; }

      public string DateTimeText { get; }

      public string EventTypeText { get; }

      public string Message { get; }

      public bool NeedsReview
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Activity.NeedsReview = value;
               OnPropertyChanged(nameof(NeedsReviewLabel));
            }
         }
      }

      public string NeedsReviewLabel => NeedsReview ? Strings.Label_NeedsReview : Strings.Label_Reviewed;

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
            ActivityEventType.ImportingDataFailed => Strings.Format(
               nameof(Strings.Activity_ImportingDataFailed),
               EnumDisplayHelper.FormatFieldValue(activity.FieldName, activity.FieldValue)),
            ActivityEventType.ExportingDataStarted => Strings.Format(nameof(Strings.Activity_ExportingDataStarted), activity.FieldValue),
            ActivityEventType.ExportingDataSucceded => Strings.Activity_ExportingDataSucceded,
            ActivityEventType.ExportingDataFailed => Strings.Format(
               nameof(Strings.Activity_ExportingDataFailed),
               EnumDisplayHelper.FormatFieldValue(activity.FieldName, activity.FieldValue)),
            ActivityEventType.ItemUpdated => StringsHelper.ComputeItemUpdatedStrings(activity),
            ActivityEventType.ItemAdded => StringsHelper.ComputeItemAddedStrings(activity),
            ActivityEventType.ItemDeleted => StringsHelper.ComputeItemItemDeletedStrings(activity),
            ActivityEventType.ActivityLogTampered => Strings.Format(nameof(Strings.Activity_ActivityLogTampered), activity.Username),
            _ => activity.EventType.ToString(),
         };

         message = message.Trim();
         return message.Length == 0
            ? string.Empty
            : char.ToUpperInvariant(message[0]) + message[1..];
      }
   }

   internal sealed class ActivitiesViewModel : ObservableObject
   {
      public ActivitiesViewModel()
      {
         EventTypeOptions = [.. Enum.GetValues<ActivityEventType>().Select(e => e.ToReadableString())];
         SelectedEventType = Strings.Filter_All;
         RefreshCommand = new RelayCommand(Refresh);
         BackCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
         Refresh();
      }

      public string Title => Strings.Format(nameof(Strings.Title_Activities), PasskeyAppInfo.Title);

      public ObservableCollection<ActivityRowViewModel> Activities { get; } = [];

      public IReadOnlyList<string> EventTypeOptions { get; }

      public string SelectedEventType
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Refresh();
            }
         }
      } = Strings.Filter_All;

      public string FiltersHeader => Strings.Format(
         nameof(Strings.Msg_FiltersHeader),
         Activities.Count,
         AppServices.Session.Database?.Activities?.Count() ?? 0);

      public bool NeedsReviewOnly
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Refresh();
            }
         }
      }

      public string SearchText
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               Refresh();
            }
         }
      } = string.Empty;

      public ICommand RefreshCommand { get; }
      public ICommand BackCommand { get; }

      public void Refresh()
      {
         Activities.Clear();
         IEnumerable<IActivity>? source = AppServices.Session.Database?.Activities;
         if (source is null)
         {
            OnPropertyChanged(nameof(FiltersHeader));
            return;
         }

         ActivityEventType eventFilter = EnumHelper.ActivityEventTypeFromReadableString(SelectedEventType);

         foreach (IActivity activity in source.OrderByDescending(a => a.DateTime))
         {
            if (eventFilter != ActivityEventType.None && activity.EventType != eventFilter)
            {
               continue;
            }

            ActivityRowViewModel row = new(activity);
            if (NeedsReviewOnly && !row.NeedsReview)
            {
               continue;
            }

            if (!string.IsNullOrWhiteSpace(SearchText)
                && !row.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(activity.ItemId, SearchText, StringComparison.Ordinal))
            {
               continue;
            }

            Activities.Add(row);
         }

         OnPropertyChanged(nameof(FiltersHeader));
         OnPropertyChanged(nameof(Title));
      }
   }
}
