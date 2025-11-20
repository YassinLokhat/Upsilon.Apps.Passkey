using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

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

      public string ServiceFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(ServiceFilter));

               RefreshFilters();
            }
         }
      } = string.Empty;

      public string IdentifiantFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(IdentifiantFilter));

               RefreshFilters();
            }
         }
      } = string.Empty;

      public string TextFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(TextFilter));

               RefreshFilters();
            }
         }
      } = string.Empty;

      public ObservableCollection<ServiceViewModel> Services { get; set; } = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserServicesViewModel(string defaultTitle)
      {
         _title = _defaultTitle = defaultTitle;

         RefreshFilters();

         DispatcherTimer timer = new()
         {
            Interval = new TimeSpan(0, 0, 0, 0, 500),
            IsEnabled = true,
         };

         timer.Tick += _timer_Elapsed;
      }

      public ServiceViewModel AddService()
      {
         ServiceViewModel? serviceViewModel = Services.FirstOrDefault(x => x.ServiceName == "New Service");

         if (serviceViewModel == null)
         {
            serviceViewModel = new(MainViewModel.User.AddService("New Service"));
            Services.Insert(0, serviceViewModel);
         }

         return serviceViewModel;
      }

      public int DeleteService(ServiceViewModel serviceViewModel)
      {
         int index = Services.IndexOf(serviceViewModel);

         _ = Services.Remove(serviceViewModel);
         MainViewModel.User.DeleteService(serviceViewModel.Service);

         return index < Services.Count ? index : Services.Count - 1;
      }

      public void RefreshFilters()
      {
         Services.Clear();

         ServiceViewModel[] services = [.. MainViewModel.User.Services
            .Where(x => x.MeetsFilterConditions(ServiceFilter, IdentifiantFilter, TextFilter))
            .OrderBy(x => x.ServiceName)
            .Select(x => new ServiceViewModel(x, IdentifiantFilter, TextFilter))];

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
