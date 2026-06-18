using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class UserActivitiesViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      public string FiltersHeader => $"Filters : {Activities.Count} activities found over {AppServices.Session.Database?.Activities?.Length}";
      public DateTime FromDateFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(FromDateFilter));
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
               OnPropertyChanged(nameof(ToDateFilter));
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
               OnPropertyChanged(nameof(ReadableEventType));
               RefreshFilters();
            }
         }
      } = ActivityEventType.None;

      public string Message
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(Message));
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
               OnPropertyChanged(nameof(NeedsReview));
               RefreshFilters();
            }
         }
      } = false;

      public ObservableCollection<ActivityViewModel> Activities { get; set; } = [];

      public ICommand ClearFiltersCommand { get; }

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserActivitiesViewModel()
      {
         Title = AppInfo.Title + " - Activities";

         ClearFiltersCommand = new RelayCommand(ClearFilters);

         RefreshFilters();
      }

      private bool _locked = false;
      public void ClearFilters()
      {
         _locked = true;

         FromDateFilter = ToDateFilter = DateTime.Now.Date.AddDays(1);
         EventType = ActivityEventType.None;
         Message = string.Empty;
         NeedsReview = false;

         _locked = false;

         RefreshFilters();
      }

      public void RefreshFilters(string itemId = "")
      {
         if (_locked) return;

         Activities.Clear();

         if (AppServices.Session.Database?.Activities is null) return;

         ActivityViewModel[] activities = [.. AppServices.Session.Database.Activities
            .Select(x => new ActivityViewModel(x))
            .Where(x => x.MeetsConditions(itemId, FromDateFilter, ToDateFilter, EventType, Message, NeedsReview))
            .OrderByDescending(x => x.DateTime)];

         foreach (ActivityViewModel activity in activities)
         {
            Activities.Add(activity);
         }

         OnPropertyChanged(nameof(FiltersHeader));
      }
   }
}
