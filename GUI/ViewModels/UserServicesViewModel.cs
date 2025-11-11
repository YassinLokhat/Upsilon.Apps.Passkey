using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserServicesViewModel : INotifyPropertyChanged
   {
      private readonly string _defaultTitle;

      private string _title;
      public string Title
      {
         get => _title;
         set => PropertyHelper.SetProperty(ref _title, value, this, PropertyChanged);
      }

      private bool _isEnabled = true;
      public bool IsEnabled
      {
         get => _isEnabled;
         set => PropertyHelper.SetProperty(ref _isEnabled, value, this, PropertyChanged);
      }

      private string _serviceFilter = string.Empty;
      public string ServiceFilter
      {
         get => _serviceFilter;
         set
         {
            if (_serviceFilter != value)
            {
               _serviceFilter = value;
               OnPropertyChanged(nameof(ServiceFilter));

               Refresh();
            }
         }
      }

      private string _identifiantFilter = string.Empty;
      public string IdentifiantFilter
      {
         get => _identifiantFilter;
         set
         {
            if (_identifiantFilter != value)
            {
               _identifiantFilter = value;
               OnPropertyChanged(nameof(IdentifiantFilter));

               Refresh();
            }
         }
      }

      private string _textFilter = string.Empty;
      public string TextFilter
      {
         get => _textFilter;
         set
         {
            if (_textFilter != value)
            {
               _textFilter = value;
               OnPropertyChanged(nameof(TextFilter));

               Refresh();
            }
         }
      }

      public ObservableCollection<ServiceViewModel> Services = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserServicesViewModel(string defaultTitle)
      {
         _title = _defaultTitle = defaultTitle;
         
         Refresh();

         DispatcherTimer timer = new()
         {
            Interval = new TimeSpan(0, 0, 0, 0, 500),
            IsEnabled = true,
         };

         timer.Tick += _timer_Elapsed;
      }

      public void Refresh()
      {
         Services.Clear();

         ServiceViewModel[] services = [.. MainViewModel.User.Services
            .OrderBy(x => x.ServiceName)
            .Select(x => new ServiceViewModel(x))
            .Where(x => x.MeetFilterConditions(ServiceFilter, IdentifiantFilter, TextFilter))];

         foreach (ServiceViewModel service in services)
         {
            Services.Add(service);
         }
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         string title = _defaultTitle;

         if (MainViewModel.Database is not null)
         {
            if (MainViewModel.Database.HasChanged())
            {
               title += " - *";
            }

            int sessionLeftTime = MainViewModel.Database.SessionLeftTime ?? 0;
            title += $" - Left session time : {sessionLeftTime / 60:D2}:{sessionLeftTime % 60:D2}";
         }

         Title = title;
      }
   }
}
