using System.Collections.ObjectModel;
using System.ComponentModel;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class UserActivitiesViewModel : INotifyPropertyChanged
   {
      public string Title { get; }

      public string FiltersHeader => $"Filters : {Activities.Count} activities found over {MainViewModel.Database?.Activities?.Length}";
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

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserActivitiesViewModel()
      {
         Title = MainViewModel.AppTitle + " - Activities";

         RefreshFilters();
      }

      public void RefreshFilters()
      {
         Activities.Clear();

         if (MainViewModel.Database?.Activities is null) return;

         ActivityViewModel[] activities = [.. MainViewModel.Database.Activities
            .Select(x => new ActivityViewModel(x))
            .Where(x => x.MeetsConditions(FromDateFilter, ToDateFilter, EventType, Message, NeedsReview))
            .OrderByDescending(x => x.DateTime)];

         foreach (ActivityViewModel activity in activities)
         {
            Activities.Add(activity);
         }

         OnPropertyChanged(nameof(FiltersHeader));
      }
   }
}
