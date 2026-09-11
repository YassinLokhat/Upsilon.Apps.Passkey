using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserActivitiesViewModel : ObservableObject, ILanguageAware
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_Activities), AppInfo.Title);

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

      public ICommand ClearFiltersCommand { get; }

      public UserActivitiesViewModel()
      {
         ClearFiltersCommand = new RelayCommand(ClearFilters);

         RefreshFilters();
      }

      public void OnLanguageChanged()
      {
         Title = Strings.Format(nameof(Strings.Title_Activities), AppInfo.Title);
         OnPropertyChanged(nameof(ReadableEventType));
         RefreshFilters();
      }

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

         ActivityViewModel[] activities = [.. AppServices.Session.Database.Activities
            .Select(x => new ActivityViewModel(x))
            .Where(x => x.MeetsConditions(FromDateFilter, ToDateFilter, EventType, SearchCriteria, NeedsReview))
            .OrderByDescending(x => x.DateTime)];

         foreach (ActivityViewModel activity in activities)
         {
            Activities.Add(activity);
         }

         OnPropertyChanged(nameof(FiltersHeader));
      }
   }
}
