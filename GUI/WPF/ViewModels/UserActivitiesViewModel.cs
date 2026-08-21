using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserActivitiesViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      public string FiltersHeader => Strings.Format(nameof(Strings.Msg_FiltersHeader), Activities.Count, AppServices.Session.Database?.Activities?.Count());
      public DateTime FromDateFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(FromDateFilter));
               RefreshFilters();
            }
         }
      } = DateTime.Now.Date.AddDays(1);
      public DateTime ToDateFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(ToDateFilter));
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
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(ReadableEventType));
               RefreshFilters();
            }
         }
      } = ActivityEventType.None;

      public string SearchCriteria
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(SearchCriteria));
               RefreshFilters();
            }
         }
      } = "";

      public bool NeedsReview
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               _onPropertyChanged(nameof(NeedsReview));
               RefreshFilters();
            }
         }
      }

      public ObservableCollection<ActivityViewModel> Activities { get; set; } = [];

      public ICommand ClearFiltersCommand { get; }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void _onPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserActivitiesViewModel()
      {
         Title = Strings.Format(nameof(Strings.Title_Activities), AppInfo.Title);

         ClearFiltersCommand = new RelayCommand(ClearFilters);

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

         _onPropertyChanged(nameof(FiltersHeader));
      }
   }
}
