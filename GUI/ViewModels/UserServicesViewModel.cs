using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.ViewModels
{
   internal class UserServicesViewModel : INotifyPropertyChanged
   {
      private readonly string _defaultTitle;

      public string Title
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      }

      public string ShowWarnings
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public Brush ShowWarningsColor
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = Brushes.White;

      public string ShowActivityWarnings
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public string ShowExpiredPasswordWarnings
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public string ShowDuplicatedPasswordWarnings
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public string ShowLeakedPasswordWarnings
      {
         get => field;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

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

      public string IdentifierFilter
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(IdentifierFilter));

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

      public event EventHandler? FiltersRefreshed;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public UserServicesViewModel(string defaultTitle)
      {
         Title = _defaultTitle = defaultTitle;

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

         if (serviceViewModel is null)
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
            .Where(x => x.MeetsFilterConditions(ServiceFilter, IdentifierFilter, TextFilter))
            .OrderBy(x => x.ServiceName)
            .Select(x => new ServiceViewModel(x, IdentifierFilter, TextFilter))];

         foreach (ServiceViewModel service in services)
         {
            Services.Add(service);
         }

         FiltersRefreshed?.Invoke(this, EventArgs.Empty);
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         string title = _defaultTitle;

         if (MainViewModel.Database?.User is not null)
         {
            if (MainViewModel.Database.User.HasChanged())
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
