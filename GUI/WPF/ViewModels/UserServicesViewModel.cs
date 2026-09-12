using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Views;
using Upsilon.Apps.Passkey.Interfaces.Enums;
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

      public Brush ShowWeakPasswordAlertsColor
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

      public string ShowWeakPasswordAlerts
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

      /// <summary>
      /// Identifier-type filter for the services list. Defaults to <see cref="IdentifierViewModel.AllIdentifierType"/> (never persisted).
      /// </summary>
      public IdentifierType Type
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(TypeLabel));
               _scheduleRefresh();
            }
         }
      } = IdentifierViewModel.AllIdentifierType;

      /// <summary>Glyph-only choices including All; see <see cref="IdentifierViewModel.FilterTypeChoices"/>.</summary>
      public IReadOnlyList<IdentifierTypeChoice> TypeChoices { get; } = IdentifierViewModel.FilterTypeChoices;

      /// <summary>Localized tooltip for the current <see cref="Type"/>.</summary>
      public string TypeLabel => IdentifierViewModel.GetTypeLabel(Type);

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

      public ServiceViewModel? SelectedService
      {
         get;
         set => SetProperty(ref field, value);
      }

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
      public ICommand LogoutCommand { get; }
      public ICommand AddServiceCommand { get; }
      public ICommand DeleteServiceCommand { get; }
      public ICommand ShowActivityAlertsCommand { get; }
      public ICommand ShowDuplicatedPasswordAlertsCommand { get; }
      public ICommand ShowExpiredPasswordAlertsCommand { get; }
      public ICommand ShowLeakedPasswordAlertsCommand { get; }
      public ICommand ShowWeakPasswordAlertsCommand { get; }
      public ICommand ShowSecuritySettingsAlertsCommand { get; }
      public ICommand ShowPasskeyQualityAlertsCommand { get; }

      /// <summary>
      /// Raised when <see cref="SaveCommand"/> runs; the view performs the async save.
      /// </summary>
      public event EventHandler? SaveRequested;

      /// <summary>
      /// Raised when <see cref="GeneratePasswordCommand"/> runs; the view owns the dialog and password insert.
      /// </summary>
      public event EventHandler? GeneratePasswordRequested;

      /// <summary>
      /// Raised when <see cref="FocusFilterCommand"/> runs; the view focuses the service filter box.
      /// </summary>
      public event EventHandler? FocusFilterRequested;

      /// <summary>
      /// Raised when <see cref="CopyIdentifierCommand"/> runs; the view reads the selected identifier.
      /// </summary>
      public event EventHandler? CopyIdentifierRequested;

      /// <summary>
      /// Raised when <see cref="CopyPasswordCommand"/> runs; the view reads the selected password box.
      /// </summary>
      public event EventHandler? CopyPasswordRequested;

      /// <summary>
      /// Raised when <see cref="LogoutCommand"/> runs; the view sets <c>DialogResult</c>.
      /// </summary>
      public event EventHandler? LogoutRequested;

      public event EventHandler? FiltersRefreshed;

      public UserServicesViewModel(string userDisplayName)
      {
         _userDisplayName = userDisplayName;
         Title = _defaultTitle = Strings.Format(nameof(Strings.Title_UserServices), AppInfo.Title, _userDisplayName);

         SaveCommand = new RelayCommand(() => SaveRequested?.Invoke(this, EventArgs.Empty));
         UserSettingsCommand = new RelayCommand(_openUserSettings);
         GeneratePasswordCommand = new RelayCommand(() => GeneratePasswordRequested?.Invoke(this, EventArgs.Empty));
         ShowActivitiesCommand = new RelayCommand(_showActivities);
         AppSettingsCommand = new RelayCommand(_openAppSettings);
         FocusFilterCommand = new RelayCommand(() => FocusFilterRequested?.Invoke(this, EventArgs.Empty));
         CopyIdentifierCommand = new RelayCommand(() => CopyIdentifierRequested?.Invoke(this, EventArgs.Empty));
         CopyPasswordCommand = new RelayCommand(() => CopyPasswordRequested?.Invoke(this, EventArgs.Empty));
         ClearFiltersCommand = new RelayCommand(ClearFilters);
         LogoutCommand = new RelayCommand(() => LogoutRequested?.Invoke(this, EventArgs.Empty));
         AddServiceCommand = new RelayCommand(_addService);
         DeleteServiceCommand = new RelayCommand(_deleteService);
         ShowActivityAlertsCommand = new RelayCommand(_showActivityAlerts);
         ShowDuplicatedPasswordAlertsCommand = new RelayCommand(() =>
            _ = AppServices.Dialogs.ShowSingleton(() => new DuplicatedPasswordsAlertView()));
         ShowExpiredPasswordAlertsCommand = new RelayCommand(() =>
            _showAccountPasswordAlerts(AlertKinds.PasswordUpdateReminder));
         ShowLeakedPasswordAlertsCommand = new RelayCommand(() =>
            _showAccountPasswordAlerts(AlertKinds.PasswordLeaked));
         ShowWeakPasswordAlertsCommand = new RelayCommand(() =>
            _showAccountPasswordAlerts(AlertKinds.WeakAccountPassword));
         ShowSecuritySettingsAlertsCommand = new RelayCommand(() =>
            _ = AppServices.Dialogs.ShowSingleton(() => new SecuritySettingsAlertView()));
         ShowPasskeyQualityAlertsCommand = new RelayCommand(() =>
            _ = AppServices.Dialogs.ShowSingleton(() => new PasskeyQualityAlertView()));

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
         OnPropertyChanged(nameof(TypeLabel));

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
         GeneratePasswordRequested = null;
         FocusFilterRequested = null;
         CopyIdentifierRequested = null;
         CopyPasswordRequested = null;
         LogoutRequested = null;

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
         Type = IdentifierViewModel.AllIdentifierType;
         ChangedItemsOnly = false;
      }

      public void RefreshFilters()
      {
         if (AppServices.Session.User is not { } user)
         {
            Services.Clear();
            _serviceViewModelsById.Clear();
            SelectedService = null;
            FiltersRefreshed?.Invoke(this, EventArgs.Empty);
            return;
         }

         _ensureServiceViewModels(user);

         ServiceViewModel[] visible = [.. _serviceViewModelsById.Values
            .Where(x => x.Service.MeetsFilterConditions(ServiceFilter, IdentifierFilter, TextFilter, ChangedItemsOnly, Type))
            .OrderBy(x => x.Service.ServiceName)];

         foreach (ServiceViewModel serviceViewModel in visible)
         {
            serviceViewModel.ApplyFilters(IdentifierFilter, TextFilter, ChangedItemsOnly, Type);
         }

         string? selectedId = SelectedService?.Service.ItemId;

         Services.Clear();

         foreach (ServiceViewModel serviceViewModel in visible)
         {
            Services.Add(serviceViewModel);
         }

         SelectedService = selectedId is not null
            ? Services.FirstOrDefault(x => x.Service.ItemId == selectedId) ?? Services.FirstOrDefault()
            : Services.FirstOrDefault();

         FiltersRefreshed?.Invoke(this, EventArgs.Empty);
      }

      private void _openUserSettings()
      {
         UserSettingsView.ShowUserSettings();
         RefreshFilters();
      }

      private void _openAppSettings()
      {
         AppSettingsView.ShowAppSettings();
         RefreshFilters();
      }

      private static void _showActivities()
      {
         _ = AppServices.Dialogs.ShowSingleton(
            factory: () => new UserActivitiesView(needsReviewFilter: false),
            configure: view =>
            {
               if (view.DataContext is UserActivitiesViewModel vm)
               {
                  vm.NeedsReview = false;
               }
            });
      }

      private static void _showActivityAlerts()
      {
         _ = AppServices.Dialogs.ShowSingleton(
            factory: () => new UserActivitiesView(needsReviewFilter: true),
            configure: view =>
            {
               if (view.DataContext is UserActivitiesViewModel vm)
               {
                  vm.NeedsReview = true;
               }
            });
      }

      private static void _showAccountPasswordAlerts(string kind)
      {
         _ = AppServices.Dialogs.ShowSingleton(
            factory: () => new AccountPasswordsAlertView(kind),
            configure: view =>
            {
               if (view.DataContext is AccountPasswordsAlertViewModel vm)
               {
                  vm.Kind = kind;
               }
            });
      }

      private void _addService()
         => SelectedService = AddService();

      private void _deleteService()
      {
         if (SelectedService is not { } serviceViewModel
            || AppServices.Dialogs.Confirm(
               Strings.Format(nameof(Strings.Msg_DeleteService), serviceViewModel.ServiceDisplay),
               Strings.Title_DeleteService) != MessageBoxResult.Yes)
         {
            return;
         }

         int index = DeleteService(serviceViewModel);
         SelectedService = index >= 0 && index < Services.Count ? Services[index] : null;
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
