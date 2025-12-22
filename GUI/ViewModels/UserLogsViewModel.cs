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

      public ObservableCollection<ILog> Logs { get; set; } = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserLogsViewModel()
      {
         Title = MainViewModel.AppTitle + " - Logs";
      }

      public void RefreshFilters()
      {
         Logs.Clear();

         if (MainViewModel.Database?.Logs is null) return;

         ILog[] logs = [.. MainViewModel.Database.Logs
            .Where(x => _meetsConditions(x))
            .OrderByDescending(x => x.DateTime)];
         
         foreach (ILog log in logs)
         {
            Logs.Add(log);
         }
      }

      private bool _meetsConditions(ILog log)
      {
         if (FromDateFilter.Date <= DateTime.Now.Date
            && log.DateTime.Date < FromDateFilter.Date)
         {
            return false;
         }

         if (ToDateFilter.Date <= DateTime.Now.Date
            && log.DateTime.Date > ToDateFilter.Date)
         {
            return false;
         }

         if (EventType != LogEventType.None
            && log.EventType != EventType)
         {
            return false;
         }

         return log.Message.Contains(Message, StringComparison.CurrentCultureIgnoreCase);
      }
   }
}
