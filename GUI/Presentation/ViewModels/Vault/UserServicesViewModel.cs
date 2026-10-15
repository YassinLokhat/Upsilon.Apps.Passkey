using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;


namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault
{
   internal sealed class UserServicesViewModel : ObservableObject, IDisposable, ILanguageRefreshable, IThemeRefreshable
   {
      private static readonly TimeSpan _filterDebounce = TimeSpan.FromMilliseconds(250);

      private string _defaultTitle;
      private readonly string _userDisplayName;
      private readonly IUiTimer _titleTimer;
      private readonly IUiTimer _filterDebounceTimer;
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

      public AlertSeverity ShowAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowActivityAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowExpiredPasswordAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowDuplicatedPasswordAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowLeakedPasswordAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowWeakPasswordAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowSecuritySettingsAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

      public AlertSeverity ShowPasskeyQualityAlertsSeverity
      {
         get;
         set => SetProperty(ref field, value);
      } = AlertSeverity.Info;

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
               OnPropertyChanged(nameof(SelectedTypeChoice));
               _scheduleRefresh();
            }
         }
      } = IdentifierViewModel.AllIdentifierType;

      /// <summary>Glyph-only choices including All; see <see cref="IdentifierViewModel.FilterTypeChoices"/>.</summary>
      public IReadOnlyList<IdentifierTypeChoice> TypeChoices { get; } = IdentifierViewModel.FilterTypeChoices;

      /// <summary><see cref="Type"/> as an item of <see cref="TypeChoices"/>, for pickers without a value path.</summary>
      public IdentifierTypeChoice SelectedTypeChoice
      {
         get => TypeChoices.FirstOrDefault(c => c.Type == Type) ?? TypeChoices[0];
         set
         {
            if (value is not null)
            {
               Type = value.Type;
            }
         }
      }

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
      public ICommand UndoCommand { get; }
      public ICommand RedoCommand { get; }
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
         Title = _defaultTitle = Strings.Format(nameof(Strings.Title_UserServices), AppServices.Info.Title, _userDisplayName);

         SaveCommand = new RelayCommand(() => SaveRequested?.Invoke(this, EventArgs.Empty));
         UndoCommand = new RelayCommand(_undo, () => AppServices.Session.Database?.EditHistory.CanUndo == true);
         RedoCommand = new RelayCommand(_redo, () => AppServices.Session.Database?.EditHistory.CanRedo == true);
         UserSettingsCommand = new AsyncRelayCommand(() => _openSettingsAsync(new UserSettingsRequest()));

         if (AppServices.Session.Database is { } database)
         {
            database.EditHistory.HistoryChanged += _onEditHistoryChanged;
         }
         GeneratePasswordCommand = new RelayCommand(() => GeneratePasswordRequested?.Invoke(this, EventArgs.Empty));
         ShowActivitiesCommand = new AsyncRelayCommand(() => _showAsync(new ActivitiesRequest(NeedsReview: false)));
         AppSettingsCommand = new AsyncRelayCommand(() => _openSettingsAsync(new AppSettingsRequest()));
         FocusFilterCommand = new RelayCommand(() => FocusFilterRequested?.Invoke(this, EventArgs.Empty));
         CopyIdentifierCommand = new RelayCommand(() => CopyIdentifierRequested?.Invoke(this, EventArgs.Empty));
         CopyPasswordCommand = new RelayCommand(() => CopyPasswordRequested?.Invoke(this, EventArgs.Empty));
         ClearFiltersCommand = new RelayCommand(ClearFilters);
         LogoutCommand = new RelayCommand(() => LogoutRequested?.Invoke(this, EventArgs.Empty));
         AddServiceCommand = new RelayCommand(_addService);
         DeleteServiceCommand = new AsyncRelayCommand(_deleteServiceAsync);
         ShowActivityAlertsCommand = new AsyncRelayCommand(() => _showAsync(new ActivitiesRequest(NeedsReview: true)));
         ShowDuplicatedPasswordAlertsCommand = new AsyncRelayCommand(() => _showAsync(new DuplicatedPasswordsAlertRequest()));
         ShowExpiredPasswordAlertsCommand = new AsyncRelayCommand(() =>
            _showAsync(new AccountPasswordsAlertRequest(AlertKinds.PasswordUpdateReminder)));
         ShowLeakedPasswordAlertsCommand = new AsyncRelayCommand(() =>
            _showAsync(new AccountPasswordsAlertRequest(AlertKinds.PasswordLeaked)));
         ShowWeakPasswordAlertsCommand = new AsyncRelayCommand(() =>
            _showAsync(new AccountPasswordsAlertRequest(AlertKinds.WeakAccountPassword)));
         ShowSecuritySettingsAlertsCommand = new AsyncRelayCommand(() => _showAsync(new SecuritySettingsAlertRequest()));
         ShowPasskeyQualityAlertsCommand = new AsyncRelayCommand(() => _showAsync(new PasskeyQualityAlertRequest()));

         RefreshFilters();

         IUiTimerFactory timers = AppServices.Timers
            ?? throw new InvalidOperationException("No UI timer factory registered.");

         _titleTimer = timers.Create();
         _titleTimer.Interval = TimeSpan.FromMilliseconds(500);
         _titleTimer.Tick += _onTitleTimerElapsed;
         _titleTimer.Start();

         _filterDebounceTimer = timers.Create();
         _filterDebounceTimer.Interval = _filterDebounce;
         _filterDebounceTimer.Tick += _onFilterDebounceElapsed;
      }

      public void OnLanguageChanged()
      {
         if (_disposed)
         {
            return;
         }

         _defaultTitle = Strings.Format(nameof(Strings.Title_UserServices), AppServices.Info.Title, _userDisplayName);
         _refreshWindowTitle();
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

         OnPropertyChanged(nameof(ShowAlertsSeverity));
         OnPropertyChanged(nameof(ShowActivityAlertsSeverity));
         OnPropertyChanged(nameof(ShowExpiredPasswordAlertsSeverity));
         OnPropertyChanged(nameof(ShowDuplicatedPasswordAlertsSeverity));
         OnPropertyChanged(nameof(ShowLeakedPasswordAlertsSeverity));
         OnPropertyChanged(nameof(ShowWeakPasswordAlertsSeverity));
         OnPropertyChanged(nameof(ShowSecuritySettingsAlertsSeverity));
         OnPropertyChanged(nameof(ShowPasskeyQualityAlertsSeverity));

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

         _titleTimer.StopTimer();
         _titleTimer.Tick -= _onTitleTimerElapsed;
         _titleTimer.Dispose();

         _filterDebounceTimer.StopTimer();
         _filterDebounceTimer.Tick -= _onFilterDebounceElapsed;
         _filterDebounceTimer.Dispose();

         // Drop UI listeners so EndSession's app language/theme Apply cannot
         // refresh a closing UserServicesView (alerts menu reorder would throw).
         if (AppServices.Session.Database is { } database)
         {
            database.EditHistory.HistoryChanged -= _onEditHistoryChanged;
         }

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

      public bool IsSaving
      {
         get;
         private set => SetProperty(ref field, value);
      }

      public bool HasAlerts
      {
         get;
         private set => SetProperty(ref field, value);
      }

      /// <summary>Alert groups in display order, empty groups included.</summary>
      public IReadOnlyList<AlertGroupSummary> AlertGroups
      {
         get;
         private set => SetProperty(ref field, value);
      } = [];

      /// <summary>Selected identifier of the selected account, or its first identifier.</summary>
      public string? CurrentIdentifier
      {
         get
         {
            AccountViewModel? account = SelectedService?.SelectedAccount;
            return account?.SelectedIdentifier?.Identifier ?? account?.Identifiers.FirstOrDefault()?.Identifier;
         }
      }

      public string? CurrentPassword => SelectedService?.SelectedAccount?.Password;

      public ICommand CommandFor(AlertGroup group) => group switch
      {
         AlertGroup.LeakedPasswords => ShowLeakedPasswordAlertsCommand,
         AlertGroup.PasskeyQuality => ShowPasskeyQualityAlertsCommand,
         AlertGroup.ExpiredPasswords => ShowExpiredPasswordAlertsCommand,
         AlertGroup.WeakPasswords => ShowWeakPasswordAlertsCommand,
         AlertGroup.ActivityReview => ShowActivityAlertsCommand,
         AlertGroup.DuplicatedPasswords => ShowDuplicatedPasswordAlertsCommand,
         _ => ShowSecuritySettingsAlertsCommand,
      };

      public void ApplyAlertSummary(AlertSummary summary)
      {
         ArgumentNullException.ThrowIfNull(summary);

         if (_disposed)
         {
            return;
         }

         ShowAlerts = Strings.Format(nameof(Strings.Msg_ShowAlerts), summary.TotalCount);
         ShowAlertsSeverity = summary.Severity;
         ShowActivityAlertsSeverity = summary[AlertGroup.ActivityReview].Severity;
         ShowExpiredPasswordAlertsSeverity = summary[AlertGroup.ExpiredPasswords].Severity;
         ShowDuplicatedPasswordAlertsSeverity = summary[AlertGroup.DuplicatedPasswords].Severity;
         ShowLeakedPasswordAlertsSeverity = summary[AlertGroup.LeakedPasswords].Severity;
         ShowWeakPasswordAlertsSeverity = summary[AlertGroup.WeakPasswords].Severity;
         ShowSecuritySettingsAlertsSeverity = summary[AlertGroup.SecuritySettings].Severity;
         ShowPasskeyQualityAlertsSeverity = summary[AlertGroup.PasskeyQuality].Severity;
         ShowActivityAlerts = Strings.Format(nameof(Strings.Msg_ShowActivityAlerts), summary[AlertGroup.ActivityReview].Count);
         ShowExpiredPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowExpiredPasswordAlerts), summary[AlertGroup.ExpiredPasswords].Count);
         ShowDuplicatedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowDuplicatedPasswordAlerts), summary[AlertGroup.DuplicatedPasswords].Count);
         ShowLeakedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowLeakedPasswordAlerts), summary[AlertGroup.LeakedPasswords].Count);
         ShowWeakPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowWeakPasswordAlerts), summary[AlertGroup.WeakPasswords].Count);
         ShowSecuritySettingsAlerts = Strings.Format(nameof(Strings.Msg_ShowSecuritySettingsAlerts), summary[AlertGroup.SecuritySettings].Count);
         ShowPasskeyQualityAlerts = Strings.Format(nameof(Strings.Msg_ShowPasskeyQualityAlerts), summary[AlertGroup.PasskeyQuality].Count);
         HasAlerts = summary.TotalCount != 0;
         AlertGroups = summary.OrderedGroups;
      }

      /// <summary>
      /// Saves the vault, rebuilds the list and restores the service and account selection.
      /// </summary>
      public async Task SaveAsync()
      {
         if (IsSaving || AppServices.Session.Database is not { } database)
         {
            return;
         }

         string? serviceId = SelectedService?.Service.ItemId;
         string? accountId = SelectedService?.SelectedAccount?.Account.ItemId;

         IsSaving = true;
         try
         {
            await database.SaveAsync().ConfigureAwait(true);
         }
         finally
         {
            IsSaving = false;
         }

         if (_disposed)
         {
            return;
         }

         RefreshFilters();
         SelectedService = Services.FirstOrDefault(x => x.Service.ItemId == serviceId);

         if (SelectedService is { } service && !string.IsNullOrEmpty(accountId))
         {
            service.SelectedAccount = service.Accounts.FirstOrDefault(x => x.Account.ItemId == accountId);
         }
      }

      /// <summary>
      /// Clears the filters and selects the service or account with <paramref name="itemId"/>.
      /// </summary>
      public bool TrySelectItem(string itemId)
      {
         if (string.IsNullOrEmpty(itemId))
         {
            return false;
         }

         ClearFilters();
         _filterDebounceTimer.StopTimer();
         RefreshFilters();

         switch (itemId[0])
         {
            case 'S':
               SelectedService = Services.FirstOrDefault(x => x.Service.ItemId == itemId);
               break;
            case 'A':
               SelectedService = Services.FirstOrDefault(x => x.Service.Accounts.Any(y => y.ItemId == itemId));
               if (SelectedService is { } service)
               {
                  service.SelectedAccount = service.Accounts.FirstOrDefault(x => x.Account.ItemId == itemId);
               }
               break;
            default:
               SelectedService = null;
               break;
         }

         return SelectedService is not null;
      }

      /// <summary>Opens the generator and writes the result into the selected account.</summary>
      public async Task GeneratePasswordIntoSelectionAsync()
      {
         if (SelectedService?.SelectedAccount is not { } account || AppServices.Dialogs is not { } dialogs)
         {
            return;
         }

         string? password = await dialogs.ShowAsync(new GeneratePasswordRequest()).ConfigureAwait(true);

         if (!string.IsNullOrEmpty(password) && !_disposed)
         {
            account.Password = password;
         }
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

      private async Task _openSettingsAsync<TRequest>(TRequest request)
         where TRequest : IViewRequest<bool>
      {
         await _showAsync(request).ConfigureAwait(true);
         // Re-assert effective culture/theme after the modal: nested dialogs and
         // MainWindow restore paths can briefly apply the app preference alone.
         AppServices.Session.ApplySessionLanguage();
         AppServices.Session.ApplySessionTheme();
         RefreshFilters();
      }

      private void _raiseHistoryCommands()
      {
         ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
         ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
      }

      private static IDialogService _dialogs
         => AppServices.Dialogs ?? throw new InvalidOperationException("No dialog service registered.");

      private static async Task _showAsync(IViewRequest<bool> request)
         => _ = await _dialogs.ShowAsync(request).ConfigureAwait(true);

      private void _undo()
      {
         AppServices.Session.Database?.EditHistory.Undo();
         _reloadAfterHistory();
      }

      private void _redo()
      {
         AppServices.Session.Database?.EditHistory.Redo();
         _reloadAfterHistory();
      }

      private void _reloadAfterHistory()
      {
         string? selectedServiceId = SelectedService?.Service.ItemId;
         string? selectedAccountId = SelectedService?.SelectedAccount?.Account.ItemId;

         // Force AccountView to rebuild identifier rows / password box from the model.
         foreach (ServiceViewModel service in _serviceViewModelsById.Values)
         {
            service.SyncFromModel();
         }

         RefreshFilters();

         if (selectedServiceId is not null)
         {
            SelectedService = Services.FirstOrDefault(x => x.Service.ItemId == selectedServiceId)
               ?? SelectedService;
         }

         if (SelectedService is not null
            && selectedAccountId is not null)
         {
            SelectedService.SelectedAccount = SelectedService.Accounts
               .FirstOrDefault(x => x.Account.ItemId == selectedAccountId)
               ?? SelectedService.SelectedAccount;
         }

         AppServices.Session.Database?.RefreshAlerts();
         _raiseHistoryCommands();
      }

      private void _onEditHistoryChanged(object? sender, EventArgs e)
         => _raiseHistoryCommands();

      private void _addService()
         => SelectedService = AddService();

      private async Task _deleteServiceAsync()
      {
         if (SelectedService is not { } serviceViewModel
            || await _dialogs.ConfirmAsync(
               Strings.Format(nameof(Strings.Msg_DeleteService), serviceViewModel.ServiceDisplay),
               Strings.Title_DeleteService).ConfigureAwait(true) != DialogResult.Yes)
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
         _filterDebounceTimer.StopTimer();
         _filterDebounceTimer.Start();
      }

      private void _onFilterDebounceElapsed(object? sender, EventArgs e)
      {
         _filterDebounceTimer.StopTimer();
         RefreshFilters();
      }

      private void _onTitleTimerElapsed(object? sender, EventArgs e)
         => _refreshWindowTitle();

      private void _refreshWindowTitle()
      {
         // Rebuild the base from CurrentUICulture every tick so a culture change
         // that missed OnLanguageChanged cannot leave a stale-language title for
         // up to one timer interval (500 ms).
         _defaultTitle = Strings.Format(
            nameof(Strings.Title_UserServices),
            AppServices.Info.Title,
            _userDisplayName);

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
