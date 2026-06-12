using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class UserServicesViewModel : INotifyPropertyChanged, IDisposable
   {
      private readonly string _defaultTitle;
      private readonly DispatcherTimer _titleTimer;
      private bool _disposed;

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
      } = SemanticBrushes.Info;

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

      public bool ChangedItemsOnly
      {
         get;
         set
         {
            if (field != value)
            {
               field = value;
               OnPropertyChanged(nameof(ChangedItemsOnly));

               RefreshFilters();
            }
         }
      } = false;

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

         _titleTimer = new DispatcherTimer
         {
            Interval = new TimeSpan(0, 0, 0, 0, 500),
            IsEnabled = true,
         };

         _titleTimer.Tick += _timer_Elapsed;
      }

      public void Dispose()
      {
         if (_disposed) return;

         _titleTimer.Stop();
         _titleTimer.Tick -= _timer_Elapsed;
         _disposed = true;
         GC.SuppressFinalize(this);
      }

      public ServiceViewModel AddService()
      {
         ServiceViewModel? serviceViewModel = Services.FirstOrDefault(x => x.ServiceName.StartsWith("New Service #"));

         if (serviceViewModel is null && AppServices.Session.User is { } user)
         {
            serviceViewModel = new(user.AddService("New Service #" + DateTime.Now.Ticks));
            Services.Insert(0, serviceViewModel);
         }

         return serviceViewModel!;
      }

      public int DeleteService(ServiceViewModel serviceViewModel)
      {
         int index = Services.IndexOf(serviceViewModel);

         _ = Services.Remove(serviceViewModel);
         AppServices.Session.User?.DeleteService(serviceViewModel.Service);

         return index < Services.Count ? index : Services.Count - 1;
      }

      public void RefreshFilters()
      {
         Services.Clear();

         if (AppServices.Session.User is not { } user)
         {
            FiltersRefreshed?.Invoke(this, EventArgs.Empty);
            return;
         }

         ServiceViewModel[] services = [.. user.Services
            .Where(x => x.MeetsFilterConditions(ServiceFilter, IdentifierFilter, TextFilter, ChangedItemsOnly))
            .OrderBy(x => x.ServiceName)
            .Select(x => new ServiceViewModel(x, IdentifierFilter, TextFilter, ChangedItemsOnly))];

         foreach (ServiceViewModel service in services)
         {
            Services.Add(service);
         }

         FiltersRefreshed?.Invoke(this, EventArgs.Empty);
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         string title = _defaultTitle;

         if (AppServices.Session.Database?.User is { } user)
         {
            if (user.HasChanged())
            {
               title += " - *";
            }

            int sessionLeftTime = AppServices.Session.Database.SessionLeftTime ?? 0;
            title += $" - Left session time : {sessionLeftTime / 60:D2}:{sessionLeftTime % 60:D2}";
         }

         Title = title;
      }
   }
}
