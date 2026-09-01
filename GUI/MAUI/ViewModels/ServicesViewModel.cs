using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class ServiceItemViewModel : ObservableObject
   {
      public ServiceItemViewModel(IService service)
      {
         Service = service;
         Refresh();
      }

      public IService Service { get; }

      public string DisplayName
      {
         get;
         private set => SetProperty(ref field, value);
      } = string.Empty;

      public string ServiceName
      {
         get => Service.ServiceName;
         set
         {
            if (Service.ServiceName != value)
            {
               Service.ServiceName = value;
               Refresh();
            }
         }
      }

      public string Url
      {
         get => Service.Url?.OriginalString ?? string.Empty;
         set
         {
            string current = Service.Url?.OriginalString ?? string.Empty;
            if (current != value)
            {
               Service.Url = string.IsNullOrWhiteSpace(value) ? null : new Uri(value, UriKind.RelativeOrAbsolute);
               OnPropertyChanged();
            }
         }
      }

      public string Notes
      {
         get => Service.Notes;
         set
         {
            if (Service.Notes != value)
            {
               Service.Notes = value;
               OnPropertyChanged();
            }
         }
      }

      public void Refresh()
      {
         DisplayName = $"{(Service.HasChanged() ? "* " : string.Empty)}{Service.ServiceName}";
         OnPropertyChanged(nameof(ServiceName));
         OnPropertyChanged(nameof(Url));
         OnPropertyChanged(nameof(Notes));
      }
   }

   internal sealed class PasswordHistoryItemViewModel(string when, string password)
   {
      public string When { get; } = when;
      public string Password { get; } = password;
      public string Display => string.IsNullOrEmpty(Password) ? When : $"{When}: ••••";
   }

   internal sealed class AccountItemViewModel : ObservableObject
   {
      public AccountItemViewModel(IAccount account)
      {
         Account = account;
         Refresh();
      }

      public IAccount Account { get; }

      public string DisplayName
      {
         get;
         private set => SetProperty(ref field, value);
      } = string.Empty;

      public string Label
      {
         get => Account.Label;
         set
         {
            if (Account.Label != value)
            {
               Account.Label = value;
               Refresh();
            }
         }
      }

      public string IdentifiersText
      {
         get => string.Join(Environment.NewLine, Account.Identifiers);
         set
         {
            string[] ids = value
               .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Account.Identifiers = ids;
            Refresh();
         }
      }

      public string Password
      {
         get => Account.Password;
         set
         {
            if (Account.Password != value)
            {
               Account.Password = value;
               OnPropertyChanged();
               Refresh();
            }
         }
      }

      public string Notes
      {
         get => Account.Notes;
         set
         {
            if (Account.Notes != value)
            {
               Account.Notes = value;
               OnPropertyChanged();
            }
         }
      }

      public int RemindPasswordUpdateDelay
      {
         get => Account.PasswordUpdateReminderDelay;
         set
         {
            if (Account.PasswordUpdateReminderDelay != value)
            {
               Account.PasswordUpdateReminderDelay = value;
               OnPropertyChanged();
               OnPropertyChanged(nameof(RemindPasswordUpdate));
               OnPropertyChanged(nameof(RemindPasswordUpdateDelayText));
            }
         }
      }

      public string RemindPasswordUpdateDelayText
      {
         get => RemindPasswordUpdateDelay.ToString(CultureInfo.InvariantCulture);
         set
         {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= 0)
            {
               RemindPasswordUpdateDelay = parsed;
            }
         }
      }

      public bool RemindPasswordUpdate
      {
         get => RemindPasswordUpdateDelay != 0;
         set
         {
            if (RemindPasswordUpdate != value)
            {
               RemindPasswordUpdateDelay = value ? 2 : 0;
               OnPropertyChanged();
            }
         }
      }

      public bool WarnPasswordLeak
      {
         get => Account.Options.HasFlag(AccountOption.WarnIfPasswordLeaked);
         set
         {
            if (WarnPasswordLeak == value)
            {
               return;
            }

            if (value)
            {
               Account.Options |= AccountOption.WarnIfPasswordLeaked;
            }
            else
            {
               Account.Options &= ~AccountOption.WarnIfPasswordLeaked;
            }

            OnPropertyChanged();
         }
      }

      public bool WarnIfDuplicatedPassword
      {
         get => Account.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword);
         set
         {
            if (WarnIfDuplicatedPassword == value)
            {
               return;
            }

            if (value)
            {
               Account.Options |= AccountOption.WarnIfDuplicatedPassword;
            }
            else
            {
               Account.Options &= ~AccountOption.WarnIfDuplicatedPassword;
            }

            OnPropertyChanged();
         }
      }

      public ObservableCollection<PasswordHistoryItemViewModel> PasswordHistory { get; } = [];

      public bool HasPasswordHistory => PasswordHistory.Count > 0;

      public string FirstIdentifier => Account.Identifiers.FirstOrDefault() ?? string.Empty;

      public void Refresh()
      {
         string summary = $"{Account.Label} {FirstIdentifier}".Trim();
         DisplayName = $"{(Account.HasChanged() ? "* " : string.Empty)}{summary}";
         OnPropertyChanged(nameof(Label));
         OnPropertyChanged(nameof(IdentifiersText));
         OnPropertyChanged(nameof(Password));
         OnPropertyChanged(nameof(Notes));
         OnPropertyChanged(nameof(FirstIdentifier));
         OnPropertyChanged(nameof(RemindPasswordUpdateDelay));
         OnPropertyChanged(nameof(RemindPasswordUpdate));
         OnPropertyChanged(nameof(WarnPasswordLeak));
         OnPropertyChanged(nameof(WarnIfDuplicatedPassword));

         PasswordHistory.Clear();
         foreach (KeyValuePair<DateTime, string> entry in Account.Passwords.OrderByDescending(x => x.Key))
         {
            if (string.IsNullOrEmpty(entry.Value))
            {
               continue;
            }

            string when = entry.Key.ToString(Strings.Activity_DateTimeFormat, CultureInfo.InvariantCulture);
            PasswordHistory.Add(new PasswordHistoryItemViewModel(when, entry.Value));
         }

         OnPropertyChanged(nameof(HasPasswordHistory));
      }
   }

   internal sealed class ServicesViewModel : ObservableObject
   {
      private readonly AsyncRelayCommand _logoutCommand;
      private readonly AsyncRelayCommand _saveCommand;
      private IDispatcherTimer? _sessionTimer;
      private List<ServiceItemViewModel> _allServices = [];
      private static string? _pendingServiceItemId;
      private static string? _pendingAccountItemId;

      public ServicesViewModel()
      {
         _logoutCommand = new AsyncRelayCommand(_logoutAsync);
         _saveCommand = new AsyncRelayCommand(_saveAsync);
         AddServiceCommand = new RelayCommand(_addService);
         AddAccountCommand = new RelayCommand(_addAccount, () => SelectedService is not null);
         DeleteServiceCommand = new AsyncRelayCommand(_deleteServiceAsync, () => SelectedService is not null);
         DeleteAccountCommand = new AsyncRelayCommand(_deleteAccountAsync, () => SelectedAccount is not null);
         CopyIdentifierCommand = new RelayCommand(_copyIdentifier, () => SelectedAccount is not null);
         CopyPasswordCommand = new RelayCommand(_copyPassword, () => SelectedAccount is not null);
         ShowQrIdentifierCommand = new AsyncRelayCommand(_showQrIdentifierAsync, () => SelectedAccount is not null);
         ShowQrPasswordCommand = new AsyncRelayCommand(_showQrPasswordAsync, () => SelectedAccount is not null);
         OpenUrlCommand = new AsyncRelayCommand(_openUrlAsync, () => SelectedService is not null);
         CopyHistoryPasswordCommand = new RelayCommand(p => _copyHistoryPassword(p as PasswordHistoryItemViewModel));
         ApplyFilterCommand = new RelayCommand(_applyFilter);

         OpenUserSettingsCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoToUserSettingsAsync());
         OpenAppSettingsCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoToAppSettingsAsync());
         OpenGeneratorCommand = new AsyncRelayCommand(_openGeneratorAsync);
         OpenActivitiesCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoToActivitiesAsync());
         OpenWarningsCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoToWarningsAsync());
         InsertIdentifierCommand = new AsyncRelayCommand(_insertIdentifierAsync, () => SelectedAccount is not null);
      }

      /// <summary>Account that receives a generated password from <see cref="PasswordGeneratorViewModel"/>.</summary>
      private static IAccount? _passwordApplyTarget;

      public static void SetPasswordApplyTarget(IAccount? account) => _passwordApplyTarget = account;

      public static bool CanApplyPassword => _passwordApplyTarget is not null;

      public static bool TryApplyGeneratedPassword(string password)
      {
         if (_passwordApplyTarget is null || string.IsNullOrEmpty(password))
         {
            return false;
         }

         _passwordApplyTarget.Password = password;
         return true;
      }

      public static void ClearPasswordApplyTarget() => _passwordApplyTarget = null;

      /// <summary>
      /// Next <see cref="Load"/> selects this account (e.g. from the Warnings page).
      /// </summary>
      public static void RequestSelectAccount(IAccount account)
      {
         ArgumentNullException.ThrowIfNull(account);
         _pendingServiceItemId = account.Service.ItemId;
         _pendingAccountItemId = account.ItemId;
      }

      public string Title => Strings.Title_Services;

      public string Username => AppServices.Session.User?.Username ?? string.Empty;

      public ObservableCollection<ServiceItemViewModel> Services { get; } = [];

      public ObservableCollection<AccountItemViewModel> Accounts { get; } = [];

      public bool HasServices => Services.Count > 0;

      public string EmptyMessage => Strings.Msg_NoServices;

      public string FilterText
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _applyFilter();
            }
         }
      } = string.Empty;

      public bool FilterChangedOnly
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _applyFilter();
            }
         }
      }

      public string StatusMessage
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string SessionLeftLabel
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public string WarningsBanner
      {
         get;
         private set => SetProperty(ref field, value);
      } = string.Empty;

      public bool HasWarningsBanner => !string.IsNullOrEmpty(WarningsBanner);

      public ServiceItemViewModel? SelectedService
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _loadAccounts();
               _notifySelectionCommands();
            }
         }
      }

      public AccountItemViewModel? SelectedAccount
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(HasSelectedAccount));
               _notifySelectionCommands();
            }
         }
      }

      public bool HasSelectedAccount => SelectedAccount is not null;

      public bool HasSelectedService => SelectedService is not null;

      public ICommand LogoutCommand => _logoutCommand;
      public ICommand SaveCommand => _saveCommand;
      public ICommand AddServiceCommand { get; }
      public ICommand AddAccountCommand { get; }
      public ICommand DeleteServiceCommand { get; }
      public ICommand DeleteAccountCommand { get; }
      public ICommand CopyIdentifierCommand { get; }
      public ICommand CopyPasswordCommand { get; }
      public ICommand ShowQrIdentifierCommand { get; }
      public ICommand ShowQrPasswordCommand { get; }
      public ICommand OpenUrlCommand { get; }
      public ICommand CopyHistoryPasswordCommand { get; }
      public ICommand ApplyFilterCommand { get; }
      public ICommand OpenUserSettingsCommand { get; }
      public ICommand OpenAppSettingsCommand { get; }
      public ICommand OpenGeneratorCommand { get; }
      public ICommand OpenActivitiesCommand { get; }
      public ICommand OpenWarningsCommand { get; }
      public ICommand InsertIdentifierCommand { get; }

      public string? SelectedIdentifierForHotkey => SelectedAccount?.FirstIdentifier;
      public string? SelectedPasswordForHotkey => SelectedAccount?.Password;

      public void Load()
      {
         Services.Clear();
         Accounts.Clear();
         SelectedService = null;
         SelectedAccount = null;
         _allServices = [];

         IUser? user = AppServices.Session.User;
         if (user is null)
         {
            OnPropertyChanged(nameof(HasServices));
            OnPropertyChanged(nameof(Username));
            _refreshWarningsBanner();
            return;
         }

         foreach (IService service in user.Services.OrderBy(s => s.ServiceName))
         {
            _allServices.Add(new ServiceItemViewModel(service));
         }

         _applyFilter();
         _trySelectPending();

         OnPropertyChanged(nameof(HasServices));
         OnPropertyChanged(nameof(Username));
         OnPropertyChanged(nameof(Title));
         _refreshWarningsBanner();
         _startSessionWatch();
      }

      public void TryConsumeInsertedIdentifier()
      {
         string? inserted = InsertIdentifierViewModel.PendingResult;
         string? serviceId = InsertIdentifierViewModel.PendingServiceItemId;
         string? accountId = InsertIdentifierViewModel.PendingAccountItemId;
         if (string.IsNullOrEmpty(inserted) || string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(accountId))
         {
            return;
         }

         InsertIdentifierViewModel.ClearPendingResult();

         ServiceItemViewModel? service = _allServices.FirstOrDefault(s => s.Service.ItemId == serviceId);
         IAccount? account = service?.Service.Accounts.FirstOrDefault(a => a.ItemId == accountId);
         if (account is null)
         {
            return;
         }

         if (!Services.Contains(service!))
         {
            _applyFilter();
         }

         SelectedService = Services.FirstOrDefault(s => s.Service.ItemId == serviceId) ?? service;
         SelectedAccount = Accounts.FirstOrDefault(a => a.Account.ItemId == accountId)
            ?? new AccountItemViewModel(account);

         List<string> ids = [.. account.Identifiers];
         if (!ids.Contains(inserted, StringComparer.Ordinal))
         {
            ids.Add(inserted);
            account.Identifiers = [.. ids];
            SelectedAccount.Refresh();
            StatusMessage = Strings.Msg_Saved;
         }
      }

      public void Unload()
      {
         if (_sessionTimer is not null)
         {
            _sessionTimer.Stop();
            _sessionTimer.Tick -= _onSessionTick;
            _sessionTimer = null;
         }
      }

      public void RefreshSelected()
      {
         SelectedService?.Refresh();
         SelectedAccount?.Refresh();
         foreach (ServiceItemViewModel s in _allServices)
         {
            s.Refresh();
         }

         _refreshWarningsBanner();
      }

      private void _applyFilter()
      {
         string needle = FilterText?.Trim() ?? string.Empty;
         bool changedOnly = FilterChangedOnly;

         IEnumerable<ServiceItemViewModel> query = _allServices;
         if (changedOnly)
         {
            query = query.Where(s => s.Service.HasChanged()
               || s.Service.Accounts.Any(a => a.HasChanged()));
         }

         if (!string.IsNullOrEmpty(needle))
         {
            query = query.Where(s =>
               s.Service.ServiceName.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || (s.Service.Url?.OriginalString.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
               || s.Service.Notes.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || s.Service.Accounts.Any(a =>
                  a.Label.Contains(needle, StringComparison.OrdinalIgnoreCase)
                  || a.Notes.Contains(needle, StringComparison.OrdinalIgnoreCase)
                  || a.Identifiers.Any(id => id.Contains(needle, StringComparison.OrdinalIgnoreCase))
                  || a.Password.Contains(needle, StringComparison.OrdinalIgnoreCase)));
         }

         ServiceItemViewModel? keep = SelectedService;
         Services.Clear();
         foreach (ServiceItemViewModel vm in query)
         {
            Services.Add(vm);
         }

         if (keep is not null && Services.Contains(keep))
         {
            SelectedService = keep;
         }
         else if (Services.Count > 0)
         {
            SelectedService = Services[0];
         }
         else
         {
            SelectedService = null;
         }

         OnPropertyChanged(nameof(HasServices));
      }

      private void _trySelectPending()
      {
         string? serviceId = _pendingServiceItemId;
         string? accountId = _pendingAccountItemId;
         _pendingServiceItemId = null;
         _pendingAccountItemId = null;

         if (string.IsNullOrEmpty(serviceId))
         {
            return;
         }

         ServiceItemViewModel? service = _allServices.FirstOrDefault(s => s.Service.ItemId == serviceId);
         if (service is null)
         {
            return;
         }

         if (!Services.Contains(service))
         {
            FilterText = string.Empty;
            FilterChangedOnly = false;
            _applyFilter();
         }

         SelectedService = service;
         if (!string.IsNullOrEmpty(accountId))
         {
            SelectedAccount = Accounts.FirstOrDefault(a => a.Account.ItemId == accountId);
         }
      }

      private void _refreshWarningsBanner()
      {
         IEnumerable<IWarning>? warnings = AppServices.Session.Database?.Warnings;
         if (warnings is null)
         {
            WarningsBanner = string.Empty;
            OnPropertyChanged(nameof(HasWarningsBanner));
            return;
         }

         int leaked = 0;
         int reminder = 0;
         int duplicated = 0;
         int review = 0;
         foreach (IWarning warning in warnings)
         {
            switch (warning.WarningType)
            {
               case WarningType.PasswordLeakedWarning:
                  leaked += warning.Accounts?.Count() ?? 0;
                  break;
               case WarningType.PasswordUpdateReminderWarning:
                  reminder += warning.Accounts?.Count() ?? 0;
                  break;
               case WarningType.DuplicatedPasswordsWarning:
                  duplicated++;
                  break;
               case WarningType.ActivityReviewWarning:
                  review += warning.Accounts?.Count() ?? 0;
                  break;
            }
         }

         List<string> parts = [];
         if (leaked > 0)
         {
            parts.Add($"{Strings.Label_WarnPasswordLeak}: {leaked}");
         }

         if (reminder > 0)
         {
            parts.Add($"{Strings.Label_RemindPasswordUpdate}: {reminder}");
         }

         if (duplicated > 0)
         {
            parts.Add($"{Strings.Label_WarnDuplicatedPassword}: {duplicated}");
         }

         if (review > 0)
         {
            parts.Add($"{Strings.Label_NeedsReview}: {review}");
         }

         WarningsBanner = parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
         OnPropertyChanged(nameof(HasWarningsBanner));
      }

      private void _loadAccounts()
      {
         Accounts.Clear();
         SelectedAccount = null;
         OnPropertyChanged(nameof(HasSelectedService));

         if (SelectedService is null)
         {
            return;
         }

         foreach (IAccount account in SelectedService.Service.Accounts)
         {
            Accounts.Add(new AccountItemViewModel(account));
         }

         if (Accounts.Count > 0)
         {
            SelectedAccount = Accounts[0];
         }
      }

      private async Task _openGeneratorAsync()
      {
         SetPasswordApplyTarget(SelectedAccount?.Account);
         await AppServices.Navigation.GoToPasswordGeneratorAsync().ConfigureAwait(true);
      }

      private async Task _insertIdentifierAsync()
      {
         if (SelectedAccount?.Account is null)
         {
            return;
         }

         InsertIdentifierViewModel.BeginInsertFor(SelectedAccount.Account);
         string initial = SelectedAccount.FirstIdentifier;
         await AppServices.Navigation.GoToInsertIdentifierAsync(initial).ConfigureAwait(true);
      }

      private void _addService()
      {
         IUser? user = AppServices.Session.User;
         if (user is null)
         {
            return;
         }

         IService service = user.AddService(Strings.Msg_NewServicePrefix + DateTime.Now.Ticks);
         ServiceItemViewModel vm = new(service);
         _allServices.Insert(0, vm);
         _applyFilter();
         SelectedService = vm;
         OnPropertyChanged(nameof(HasServices));
      }

      private void _addAccount()
      {
         if (SelectedService is null)
         {
            return;
         }

         IAccount account = SelectedService.Service.AddAccount(
            [Strings.Msg_NewAccountPrefix + DateTime.Now.Ticks]);
         AccountItemViewModel vm = new(account);
         Accounts.Insert(0, vm);
         SelectedAccount = vm;
         SelectedService.Refresh();
      }

      private async Task _deleteServiceAsync()
      {
         if (SelectedService is null)
         {
            return;
         }

         bool ok = await AppServices.Dialogs
            .ConfirmAsync(Strings.Msg_DeleteService, Strings.Title_DeleteService)
            .ConfigureAwait(true);
         if (!ok)
         {
            return;
         }

         AppServices.Session.User?.DeleteService(SelectedService.Service);
         _ = _allServices.Remove(SelectedService);
         _ = Services.Remove(SelectedService);
         SelectedService = Services.FirstOrDefault();
         OnPropertyChanged(nameof(HasServices));
         _refreshWarningsBanner();
      }

      private async Task _deleteAccountAsync()
      {
         if (SelectedService is null || SelectedAccount is null)
         {
            return;
         }

         bool ok = await AppServices.Dialogs
            .ConfirmAsync(Strings.Msg_DeleteAccount, Strings.Title_DeleteAccount)
            .ConfigureAwait(true);
         if (!ok)
         {
            return;
         }

         SelectedService.Service.DeleteAccount(SelectedAccount.Account);
         _ = Accounts.Remove(SelectedAccount);
         SelectedAccount = Accounts.FirstOrDefault();
         SelectedService.Refresh();
         _refreshWarningsBanner();
      }

      private void _copyIdentifier()
      {
         string? id = SelectedAccount?.FirstIdentifier;
         if (string.IsNullOrEmpty(id))
         {
            return;
         }

         AppServices.Clipboard.SetText(id, ClipboardManager.AutoClearAfter);
         StatusMessage = Strings.Msg_Copied;
      }

      private void _copyPassword()
      {
         string? password = SelectedAccount?.Password;
         if (string.IsNullOrEmpty(password))
         {
            return;
         }

         AppServices.Clipboard.SetText(password, ClipboardManager.AutoClearAfter);
         StatusMessage = Strings.Msg_Copied;
      }

      private void _copyHistoryPassword(PasswordHistoryItemViewModel? item)
      {
         if (item is null || string.IsNullOrEmpty(item.Password))
         {
            return;
         }

         AppServices.Clipboard.SetText(item.Password, ClipboardManager.AutoClearAfter);
         StatusMessage = Strings.Msg_Copied;
      }

      private async Task _showQrIdentifierAsync()
      {
         string? id = SelectedAccount?.FirstIdentifier;
         if (string.IsNullOrEmpty(id))
         {
            return;
         }

         await AppServices.Navigation.GoToQrCodeAsync(id).ConfigureAwait(true);
      }

      private async Task _showQrPasswordAsync()
      {
         string? password = SelectedAccount?.Password;
         if (string.IsNullOrEmpty(password))
         {
            return;
         }

         await AppServices.Navigation.GoToQrCodeAsync(password).ConfigureAwait(true);
      }

      private async Task _openUrlAsync()
      {
         string? url = SelectedService?.Url;
         if (string.IsNullOrWhiteSpace(url))
         {
            return;
         }

         try
         {
            Uri uri = url.Contains("://", StringComparison.Ordinal)
               ? new Uri(url)
               : new Uri("https://" + url);
            _ = await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred).ConfigureAwait(true);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or UriFormatException
            or InvalidOperationException
            or NotSupportedException)
         {
            Log.Error(ex, "Failed to open service URL");
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
         }
      }

      private async Task _saveAsync()
      {
         IDatabase? database = AppServices.Session.Database;
         if (database is null)
         {
            return;
         }

         try
         {
            await database.SaveAsync().ConfigureAwait(true);
            StatusMessage = Strings.Msg_Saved;
            RefreshSelected();
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException)
         {
            Log.Error(ex, "Save failed");
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
         }
      }

      private void _startSessionWatch()
      {
         Unload();
         _sessionTimer = Application.Current?.Dispatcher.CreateTimer();
         if (_sessionTimer is null)
         {
            return;
         }

         _sessionTimer.Interval = TimeSpan.FromSeconds(1);
         _sessionTimer.Tick += _onSessionTick;
         _sessionTimer.Start();
      }

      private void _onSessionTick(object? sender, EventArgs e)
      {
         int? left = AppServices.Session.Database?.SessionLeftTime;
         if (left is null)
         {
            SessionLeftLabel = string.Empty;
            return;
         }

         if (left <= 0)
         {
            SessionLeftLabel = Strings.Msg_SessionEnded;
            return;
         }

         int minutes = left.Value / 60;
         int seconds = left.Value % 60;
         SessionLeftLabel = Strings.Format(nameof(Strings.Msg_SessionLeftTime), $"{minutes:00}:{seconds:00}");
      }

      private async Task _logoutAsync()
      {
         Unload();
         AppServices.Session.EndSession();
         await AppServices.Navigation.GoToLoginAsync().ConfigureAwait(true);
      }

      private void _notifySelectionCommands()
      {
         if (AddAccountCommand is RelayCommand addAccount)
         {
            addAccount.NotifyCanExecuteChanged();
         }

         if (DeleteServiceCommand is AsyncRelayCommand delService)
         {
            delService.NotifyCanExecuteChanged();
         }

         if (DeleteAccountCommand is AsyncRelayCommand delAccount)
         {
            delAccount.NotifyCanExecuteChanged();
         }

         if (CopyIdentifierCommand is RelayCommand copyId)
         {
            copyId.NotifyCanExecuteChanged();
         }

         if (CopyPasswordCommand is RelayCommand copyPw)
         {
            copyPw.NotifyCanExecuteChanged();
         }

         if (ShowQrIdentifierCommand is AsyncRelayCommand qrId)
         {
            qrId.NotifyCanExecuteChanged();
         }

         if (ShowQrPasswordCommand is AsyncRelayCommand qrPw)
         {
            qrPw.NotifyCanExecuteChanged();
         }

         if (OpenUrlCommand is AsyncRelayCommand openUrl)
         {
            openUrl.NotifyCanExecuteChanged();
         }

         if (InsertIdentifierCommand is AsyncRelayCommand insertId)
         {
            insertId.NotifyCanExecuteChanged();
         }

         OnPropertyChanged(nameof(HasSelectedService));
         OnPropertyChanged(nameof(HasSelectedAccount));
      }
   }
}
