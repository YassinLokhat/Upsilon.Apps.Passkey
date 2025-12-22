using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserLogsViewModel : INotifyPropertyChanged
   {
      public string Title { get; }
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
         set => EventType = EnumHelper.LogEventTypeFromReadableString(value);
      }
      public LogEventType EventType
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
      } = LogEventType.None;

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

      public ObservableCollection<LogViewModel> Logs { get; set; } = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserLogsViewModel()
      {
         Title = MainViewModel.AppTitle + " - Logs";
         
         RefreshFilters();
      }

      public void RefreshFilters()
      {
         Logs.Clear();

         if (MainViewModel.Database?.Logs is null) return;

         LogViewModel[] logs = [.. MainViewModel.Database.Logs
            .Select(x => new LogViewModel(x))
            .Where(x => x.MeetsConditions(FromDateFilter, ToDateFilter, EventType, Message, NeedsReview))
            .OrderByDescending(x => x.DateTime)];
         
         foreach (LogViewModel log in logs)
         {
            Logs.Add(log);
         }
      }
   }
}
