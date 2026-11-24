using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Activities
{
   internal sealed class UserActivitiesViewModel : ObservableObject, ILanguageRefreshable
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_Activities), AppServices.Info.Title);

      public string FiltersHeader => Strings.Format(nameof(Strings.Msg_FiltersHeader), Activities.Count, AppServices.Session.Database?.Activities?.Count());
      public DateTime FromDateFilter
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               RefreshFilters();
            }
         }
      } = DateTime.Now.Date.AddDays(1);
      public DateTime ToDateFilter
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               RefreshFilters();
            }
         }
      } = DateTime.Now.Date.AddDays(1);

      public string ReadableEventType
      {
         get => EventType.ToReadableString();
         set => EventType = EnumHelper.ActivityEventTypeFromReadableString(value);
      }
      public ActivityEventType EventType
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(ReadableEventType));
               RefreshFilters();
            }
         }
      } = ActivityEventType.None;

      public string SearchCriteria
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               RefreshFilters();
            }
         }
      } = "";

      public bool NeedsReview
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               RefreshFilters();
            }
         }
      }

      public ObservableCollection<ActivityViewModel> Activities { get; set; } = [];

      /// <summary>Localized event-type filter labels; <see cref="ReadableEventType"/> is one of them.</summary>
      public IReadOnlyList<string> EventTypeOptions
      {
         get;
         private set => SetProperty(ref field, value);
      } = _eventTypeOptions();

      public ActivitySortOrder SortOrder
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(SelectedSortOption));
               RefreshFilters();
            }
         }
      } = ActivitySortOrder.NewestFirst;

      /// <summary>Localized sort labels in <see cref="ActivitySortOrder"/> order.</summary>
      public IReadOnlyList<string> SortOptions
      {
         get;
         private set => SetProperty(ref field, value);
      } = _sortOptions();

      public string SelectedSortOption
      {
         get => SortOptions[(int)SortOrder];
         set
         {
            int index = SortOptions.ToList().IndexOf(value);
            if (index >= 0)
            {
               SortOrder = (ActivitySortOrder)index;
            }
         }
      }

      public ICommand ClearFiltersCommand { get; }

      public UserActivitiesViewModel()
      {
         ClearFiltersCommand = new RelayCommand(ClearFilters);

         RefreshFilters();
      }

      public void OnLanguageChanged()
      {
         Title = Strings.Format(nameof(Strings.Title_Activities), AppServices.Info.Title);
         EventTypeOptions = _eventTypeOptions();
         SortOptions = _sortOptions();
         OnPropertyChanged(nameof(ReadableEventType));
         OnPropertyChanged(nameof(SelectedSortOption));
         RefreshFilters();
      }

      private static string[] _eventTypeOptions()
         => [.. Enum.GetValues<ActivityEventType>().Select(x => x.ToReadableString())];

      private static string[] _sortOptions()
         => [
            Strings.ActivitySort_NewestFirst,
            Strings.ActivitySort_OldestFirst,
            Strings.ActivitySort_EventType,
            Strings.ActivitySort_NeedsReviewFirst,
            Strings.ActivitySort_Message,
         ];

      /// <summary>Sorts on the activity timestamp: the displayed date text is culture-formatted.</summary>
      internal static IEnumerable<ActivityViewModel> Sort(IEnumerable<ActivityViewModel> activities, ActivitySortOrder order) => order switch
      {
         ActivitySortOrder.OldestFirst => activities.OrderBy(x => x.Activity.DateTime),
         ActivitySortOrder.EventType => activities
            .OrderBy(x => x.EventType, StringComparer.CurrentCulture)
            .ThenByDescending(x => x.Activity.DateTime),
         ActivitySortOrder.NeedsReviewFirst => activities
            .OrderByDescending(x => x.NeedsReview)
            .ThenByDescending(x => x.Activity.DateTime),
         ActivitySortOrder.Message => activities
            .OrderBy(x => x.Message, StringComparer.CurrentCulture)
            .ThenByDescending(x => x.Activity.DateTime),
         _ => activities.OrderByDescending(x => x.Activity.DateTime),
      };

      private bool _locked;
      public void ClearFilters()
      {
         _locked = true;

         FromDateFilter = ToDateFilter = DateTime.Now.Date.AddDays(1);
         EventType = ActivityEventType.None;
         SearchCriteria = string.Empty;
         NeedsReview = false;

         _locked = false;

         RefreshFilters();
      }

      public void RefreshFilters()
      {
         if (_locked)
         {
            return;
         }

         Activities.Clear();

         if (AppServices.Session.Database?.Activities is null)
         {
            return;
         }

         ActivityViewModel[] activities = [.. Sort(AppServices.Session.Database.Activities
            .Select(x => new ActivityViewModel(x))
            .Where(x => x.MeetsConditions(FromDateFilter, ToDateFilter, EventType, SearchCriteria, NeedsReview)), SortOrder)];

         foreach (ActivityViewModel activity in activities)
         {
            Activities.Add(activity);
         }

         OnPropertyChanged(nameof(FiltersHeader));
      }
   }
}
