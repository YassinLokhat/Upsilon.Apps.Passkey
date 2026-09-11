using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserServicesViewModel : ObservableObject, IDisposable, ILanguageAware, IThemeAware
   {
      private static readonly TimeSpan _filterDebounce = TimeSpan.FromMilliseconds(250);

      private string _defaultTitle;
      private readonly string _userDisplayName;
      private readonly DispatcherTimer _titleTimer;
      private readonly DispatcherTimer _filterDebounceTimer;
      private bool _disposed;

      public string Title
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string UserId
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Msg_UserId), AppServices.Session.User?.ItemId);

      public string ShowAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public Brush ShowAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowActivityAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowExpiredPasswordAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowDuplicatedPasswordAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowLeakedPasswordAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowSecuritySettingsAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public Brush ShowPasskeyQualityAlertsColor
      {
         get;
         set => SetProperty(ref field, value);
      } = SemanticBrushes.Info;

      public string ShowActivityAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowExpiredPasswordAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowDuplicatedPasswordAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowLeakedPasswordAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowSecuritySettingsAlerts
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string ShowPasskeyQualityAlerts
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

      public ICommand SaveCommand { get; }
      public ICommand UserSettingsCommand { get; }
      public ICommand GeneratePasswordCommand { get; }
      public ICommand ShowActivitiesCommand { get; }
      public ICommand AppSettingsCommand { get; }
      public ICommand FocusFilterCommand { get; }
      public ICommand ClearFiltersCommand { get; }
      public ICommand CopyIdentifierCommand { get; }
      public ICommand CopyPasswordCommand { get; }

      /// <summary>
      /// Raised when <see cref="SaveCommand"/> runs; the view performs the async save.
      /// </summary>
      public event EventHandler? SaveRequested;

      /// <summary>
      /// Raised when <see cref="UserSettingsCommand"/> runs; the view opens the user settings dialog.
      /// </summary>
      public event EventHandler? UserSettingsRequested;

      /// <summary>
      /// Raised when <see cref="GeneratePasswordCommand"/> runs; the view owns the dialog and password insert.
      /// </summary>
      public event EventHandler? GeneratePasswordRequested;

      /// <summary>
      /// Raised when <see cref="ShowActivitiesCommand"/> runs; the view opens the activities window.
      /// </summary>
      public event EventHandler? ShowActivitiesRequested;

      /// <summary>
      /// Raised when <see cref="AppSettingsCommand"/> runs; the view opens the app settings dialog.
      /// </summary>
      public event EventHandler? AppSettingsRequested;

      /// <summary>
      /// Raised when <see cref="FocusFilterCommand"/> runs; the view focuses the service filter box.
      /// </summary>
      public event EventHandler? FocusFilterRequested;

      /// <summary>
      /// Raised when <see cref="CopyIdentifierCommand"/> runs; the view focuses the service filter box.
      /// </summary>
      public event EventHandler? CopyIdentifierRequested;

      /// <summary>
      /// Raised when <see cref="CopyPasswordCommand"/> runs; the view focuses the service filter box.
      /// </summary>
      public event EventHandler? CopyPasswordRequested;

      public event EventHandler? FiltersRefreshed;

      public UserServicesViewModel(string userDisplayName)
      {
         _userDisplayName = userDisplayName;
         Title = _defaultTitle = Strings.Format(nameof(Strings.Title_UserServices), AppInfo.Title, _userDisplayName);

         SaveCommand = new RelayCommand(() => SaveRequested?.Invoke(this, EventArgs.Empty));
         UserSettingsCommand = new RelayCommand(() => UserSettingsRequested?.Invoke(this, EventArgs.Empty));
         GeneratePasswordCommand = new RelayCommand(() => GeneratePasswordRequested?.Invoke(this, EventArgs.Empty));
         ShowActivitiesCommand = new RelayCommand(() => ShowActivitiesRequested?.Invoke(this, EventArgs.Empty));
         AppSettingsCommand = new RelayCommand(() => AppSettingsRequested?.Invoke(this, EventArgs.Empty));
         FocusFilterCommand = new RelayCommand(() => FocusFilterRequested?.Invoke(this, EventArgs.Empty));
         CopyIdentifierCommand = new RelayCommand(() => CopyIdentifierRequested?.Invoke(this, EventArgs.Empty));
         CopyPasswordCommand = new RelayCommand(() => CopyPasswordRequested?.Invoke(this, EventArgs.Empty));
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

      public void OnLanguageChanged()
      {
         if (_disposed)
         {
            return;
         }

         Title = _defaultTitle = Strings.Format(nameof(Strings.Title_UserServices), AppInfo.Title, _userDisplayName);
         UserId = Strings.Format(nameof(Strings.Msg_UserId), AppServices.Session.User?.ItemId);

         foreach (ServiceViewModel service in _serviceViewModelsById.Values)
         {
            service.OnLanguageChanged();
         }

         LanguageRefreshed?.Invoke(this, EventArgs.Empty);
      }

      public event EventHandler? LanguageRefreshed;

      public void OnThemeChanged()
      {
         if (_disposed)
         {
            return;
         }

         foreach (ServiceViewModel service in _serviceViewModelsById.Values)
         {
            service.OnThemeChanged();
         }

         ThemeRefreshed?.Invoke(this, EventArgs.Empty);
      }

      public event EventHandler? ThemeRefreshed;

      public void Dispose()
      {
         if (_disposed)
         {
            return;
         }

         _disposed = true;

         _titleTimer.Stop();
         _titleTimer.Tick -= _onTitleTimerElapsed;

         _filterDebounceTimer.Stop();
         _filterDebounceTimer.Tick -= _onFilterDebounceElapsed;

         // Drop UI listeners so EndSession's app language/theme Apply cannot
         // refresh a closing UserServicesView (alerts menu reorder would throw).
         LanguageRefreshed = null;
         ThemeRefreshed = null;
         FiltersRefreshed = null;
         SaveRequested = null;
         UserSettingsRequested = null;
         GeneratePasswordRequested = null;
         ShowActivitiesRequested = null;
         AppSettingsRequested = null;
         FocusFilterRequested = null;
         CopyIdentifierRequested = null;
         CopyPasswordRequested = null;

         GC.SuppressFinalize(this);
      }

      public ServiceViewModel AddService()
      {
         ServiceViewModel? serviceViewModel = Services.FirstOrDefault(x =>
            Strings.IsPlaceholderName(x.ServiceName, nameof(Strings.Msg_NewServicePrefix)));

         if (serviceViewModel is null && AppServices.Session.User is { } user)
         {
            IService service = user.AddService(Strings.Msg_NewServicePrefix + DateTime.Now.Ticks);
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

            // LogoutTimeout 0 keeps SessionLeftTime at 0 forever. Reading
            // Settings.LogoutTimeout here would go through Touch and reset the
            // idle countdown every title tick, so we key off the left-time value.
            title += sessionLeftTime == 0
               ? Strings.Msg_SessionUnlimitedTime
               : Strings.Format(nameof(Strings.Msg_SessionLeftTime), sessionLeftTime / 60, sessionLeftTime % 60);
         }

         Title = title;
      }
   }
}
