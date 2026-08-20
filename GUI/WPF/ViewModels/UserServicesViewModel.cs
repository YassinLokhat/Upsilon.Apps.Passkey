using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserServicesViewModel : ObservableObject, IDisposable
   {
      private static readonly TimeSpan _filterDebounce = TimeSpan.FromMilliseconds(250);

      private readonly string _defaultTitle;
      private readonly DispatcherTimer _titleTimer;
      private readonly DispatcherTimer _filterDebounceTimer;
      private bool _disposed;

      public string Title
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public static string UserId => $"User Id : {AppServices.Session.User?.ItemId}";

      public string ShowWarnings
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public Brush ShowWarningsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public string ShowActivityWarnings
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowExpiredPasswordWarnings
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowDuplicatedPasswordWarnings
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowLeakedPasswordWarnings
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ServiceFilter
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _scheduleRefresh();
            }
         }
      } = string.Empty;

      public string IdentifierFilter
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _scheduleRefresh();
            }
         }
      } = string.Empty;

      public string TextFilter
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _scheduleRefresh();
            }
         }
      } = string.Empty;

      public bool ChangedItemsOnly
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _scheduleRefresh();
            }
         }
      }

      public ObservableCollection<ServiceViewModel> Services { get; } = [];

      private readonly Dictionary<string, ServiceViewModel> _serviceViewModelsById = new(StringComparer.Ordinal);

      public ICommand ClearFiltersCommand { get; }

      public event EventHandler? FiltersRefreshed;

      public UserServicesViewModel(string defaultTitle)
      {
         Title = _defaultTitle = defaultTitle;

         ClearFiltersCommand = new RelayCommand(ClearFilters);

         RefreshFilters();

         _titleTimer = new DispatcherTimer
         {
            Interval = TimeSpan.FromMilliseconds(500),
            IsEnabled = true,
         };
         _titleTimer.Tick += _onTitleTimerElapsed;

         _filterDebounceTimer = new DispatcherTimer
         {
            Interval = _filterDebounce,
         };
         _filterDebounceTimer.Tick += _onFilterDebounceElapsed;
      }

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _titleTimer.Stop();
         _titleTimer.Tick -= _onTitleTimerElapsed;

         _filterDebounceTimer.Stop();
         _filterDebounceTimer.Tick -= _onFilterDebounceElapsed;

         _disposed = true;
         GC.SuppressFinalize(this);
      }

      public ServiceViewModel AddService()
      {
         ServiceViewModel? serviceViewModel = Services.FirstOrDefault(x => x.ServiceName.StartsWith("New Service #", StringComparison.Ordinal));

         if (serviceViewModel is null && AppServices.Session.User is { } user)
         {
            IService service = user.AddService("New Service #" + DateTime.Now.Ticks);
            serviceViewModel = new ServiceViewModel(service);
            _serviceViewModelsById[service.ItemId] = serviceViewModel;
            Services.Insert(0, serviceViewModel);
         }

         return serviceViewModel!;
      }

      public int DeleteService(ServiceViewModel serviceViewModel)
      {
         int index = Services.IndexOf(serviceViewModel);

         _ = Services.Remove(serviceViewModel);
         _ = _serviceViewModelsById.Remove(serviceViewModel.Service.ItemId);
         AppServices.Session.User?.DeleteService(serviceViewModel.Service);

         return index < Services.Count ? index : Services.Count - 1;
      }

      public void ClearFilters()
      {
         ServiceFilter = TextFilter = IdentifierFilter = string.Empty;
         ChangedItemsOnly = false;
      }

      public void RefreshFilters()
      {
         if (AppServices.Session.User is not { } user)
         {
            Services.Clear();
            _serviceViewModelsById.Clear();
            FiltersRefreshed?.Invoke(this, EventArgs.Empty);
            return;
         }

         _ensureServiceViewModels(user);

         ServiceViewModel[] visible = [.. _serviceViewModelsById.Values
            .Where(x => x.Service.MeetsFilterConditions(ServiceFilter, IdentifierFilter, TextFilter, ChangedItemsOnly))
            .OrderBy(x => x.Service.ServiceName)];

         foreach (ServiceViewModel serviceViewModel in visible)
         {
            serviceViewModel.ApplyFilters(IdentifierFilter, TextFilter, ChangedItemsOnly);
         }

         Services.Clear();

         foreach (ServiceViewModel serviceViewModel in visible)
         {
            Services.Add(serviceViewModel);
         }

         FiltersRefreshed?.Invoke(this, EventArgs.Empty);
      }

      private void _ensureServiceViewModels(IUser user)
      {
         HashSet<string> liveIds = [.. user.Services.Select(x => x.ItemId)];

         foreach (string id in _serviceViewModelsById.Keys.Where(k => !liveIds.Contains(k)).ToList())
         {
            _ = _serviceViewModelsById.Remove(id);
         }

         foreach (IService service in user.Services)
         {
            if (_serviceViewModelsById.ContainsKey(service.ItemId))
            {
               continue;
            }

            _serviceViewModelsById[service.ItemId] = new ServiceViewModel(service);
         }
      }

      private void _scheduleRefresh()
      {
         // Coalesce rapid keystrokes into a single RefreshFilters call so the
         // ServiceViewModel tree is not rebuilt on every character.
         _filterDebounceTimer.Stop();
         _filterDebounceTimer.Start();
      }

      private void _onFilterDebounceElapsed(object? sender, EventArgs e)
      {
         _filterDebounceTimer.Stop();
         RefreshFilters();
      }

      private void _onTitleTimerElapsed(object? sender, EventArgs e)
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
