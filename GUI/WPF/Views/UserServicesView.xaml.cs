using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Alerts;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserServicesView.xaml
   /// </summary>
   internal sealed partial class UserServicesView : Window, IDisposable
   {
      private readonly UserServicesViewModel _viewModel;
      private readonly IDatabase _database;
      private bool _isClosing;

      private static ISessionService _session => AppServices.Session;
      private static IDialogService _dialogs => AppServices.Dialogs;
      private static INavigationService _navigation => AppServices.Navigation;

      private UserServicesView()
      {
         InitializeComponent();

         _database = _session.Database
            ?? throw new InvalidOperationException("UserServicesView requires an active session.");

         _navigation.ItemRequested += _navigation_ItemRequested;

         DataContext = _viewModel = new($"{_session.User}");
         _viewModel.FiltersRefreshed += _viewModel_FiltersRefreshed;
         _viewModel.LanguageRefreshed += (_, _) => _refreshAlertsMenuFromSession();
         _viewModel.ThemeRefreshed += (_, _) => _refreshAlertsMenuFromSession();
         _viewModel.SaveRequested += (_, _) => _save();
         _viewModel.GeneratePasswordRequested += (_, _) => _generateRandomPassword();
         _viewModel.FocusFilterRequested += (_, _) => _focusServiceFilter();
         _viewModel.CopyIdentifierRequested += (_, _) => _copyIdentifierOrPassword(Key.L);
         _viewModel.CopyPasswordRequested += (_, _) => _copyIdentifierOrPassword(Key.P);
         _viewModel.LogoutRequested += (_, _) => _logout();

         if (_viewModel.SelectedService is not null)
         {
            _service_SV.SetDataContext(_viewModel.SelectedService);
         }

         _alerts_MI.Visibility = Visibility.Collapsed;

         _ = _serviceFilter_TB.Focus();

         _database.DatabaseClosed += _database_DatabaseClosed;
         _session.Alerts.NotifiedAlertsChanged += _alerts_NotifiedAlertsChanged;
         Loaded += _userServicesView_Loaded;

         IAlert[] notified = _notifiedAlerts();
         if (notified.Length != 0)
         {
            _updateAlertsMenu(notified);
         }
      }

      private void _alerts_NotifiedAlertsChanged(object? sender, EventArgs e)
      {
         _ = Dispatcher.BeginInvoke(() =>
         {
            if (_isClosing || !IsLoaded)
            {
               return;
            }

            _updateAlertsMenu(_notifiedAlerts());
         });
      }

      private void _viewModel_FiltersRefreshed(object? sender, EventArgs e)
      {
         // Selection is owned by the VM; keep the service detail pane in sync.
         _service_SV.SetDataContext(_viewModel.SelectedService);
         if (_viewModel.SelectedService is not null)
         {
            _services_LB.ScrollIntoView(_viewModel.SelectedService);
         }
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
          => this.DatabaseClosed(_isClosing);

      public static bool ShowUser(Window? owner = null)
      {
         using UserServicesView view = new();

         // Only an explicit DialogResult (Logout menu or session-timeout via
         // WindowHelper) should keep the login window open. Closing with X /
         // Alt+F4 leaves DialogResult null — that must exit the app, not
         // return to MainWindow.
         return AppServices.Dialogs.ShowDialog(view) == true;
      }

      private void _userServicesView_Loaded(object sender, RoutedEventArgs e)
      {
         this.PostLoadSetup();

         if ((_database.User?.Settings.AlertsToNotify.Count ?? 0) == 0)
         {
            _dialogs.Warn(Strings.Msg_NoAlertsToNotify, Strings.Title_NoAlertsToNotify);
         }
      }

      private void _copyIdentifierOrPassword(Key key)
      {
         string? toInsert = null;

         switch (key)
         {
            case Key.L:
               toInsert = _service_SV.GetSelectedIdentifier();
               break;
            case Key.P:
               toInsert = _service_SV.GetSelectedPassword();
               break;
         }

         if (!string.IsNullOrEmpty(toInsert))
         {
            AppServices.Clipboard.SetText(toInsert, ClipboardManager.AutoClearAfter);
         }
      }

      private void _generateRandomPassword()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         string? password = PasswordGenerator.ShowGeneratePasswordDialog(this);

         if (password is null)
         {
            return;
         }

         _service_SV.SetSelectedPassword(password);
      }

      private void _logout()
      {
         if (_isClosing || this.GetIsBusy())
         {
            return;
         }

         DialogResult = true;
      }

      private void _window_Closed(object sender, EventArgs e)
      {
         _isClosing = true;

         _database.DatabaseClosed -= _database_DatabaseClosed;
         _session.Alerts.NotifiedAlertsChanged -= _alerts_NotifiedAlertsChanged;

         _navigation.ItemRequested -= _navigation_ItemRequested;

         _dialogs.Close<AccountPasswordsAlertView>();
         _dialogs.Close<DuplicatedPasswordsAlertView>();
         _dialogs.Close<SecuritySettingsAlertView>();
         _dialogs.Close<PasskeyQualityAlertView>();
         _dialogs.Close<UserActivitiesView>();

         // Drop any PasswordBox / history plaintext before tearing down the session.
         _service_SV.SetDataContext(null);

         // Dispose the VM before EndSession: Apply(app language/theme) would
         // otherwise raise LanguageRefreshed/ThemeRefreshed and try to reorder
         // the alerts MenuItems on an already-unloaded window.
         _viewModel.Dispose();

         _session.EndSession();
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _session.User?.Shake();
         _service_SV.SetDataContext(_viewModel.SelectedService);
      }

      private async void _save()
      {
         // The busy cursor is set synchronously before the first await, so it
         // doubles as the re-entrancy guard against a second save being started
         // while this one is still running.
         if (this.GetIsBusy())
         {
            return;
         }

         string? serviceId = _service_SV.GetServiceId();
         string? accountId = _service_SV.GetAccountId();

         this.SetIsBusy(true);

         try
         {
            IDatabase? database = _session.Database;

            if (database is not null)
            {
               await database.SaveAsync().ConfigureAwait(true);
            }
         }
         finally
         {
            this.SetIsBusy(false);
         }

         if (_isClosing)
         {
            return;
         }

         _viewModel.RefreshFilters();
         _viewModel.SelectedService = _viewModel.Services.FirstOrDefault(x => x.Service.ItemId == serviceId);

         if (!string.IsNullOrEmpty(accountId))
         {
            _ = _service_SV.SelectAccount(accountId);
         }
      }

      private void _refreshAlertsMenuFromSession()
      {
         if (_isClosing || !IsLoaded)
         {
            return;
         }

         _updateAlertsMenu(_notifiedAlerts());
      }

      private static IAlert[] _notifiedAlerts()
         => [.. _session.Alerts.GetNotifiedAlerts()];

      private void _updateAlertsMenu(IAlert[] alerts)
      {
         if (_isClosing || !IsLoaded)
         {
            return;
         }

         IAlert[] activityKind = [.. alerts.Where(x => x.Kind == AlertKinds.ActivityReview)];
         IAlert[] expiredKind = [.. alerts.Where(x => x.Kind == AlertKinds.PasswordUpdateReminder)];
         IAlert[] duplicatedKind = [.. alerts.Where(x => x.Kind == AlertKinds.DuplicatedPasswords)];
         IAlert[] leakedKind = [.. alerts.Where(x => x.Kind == AlertKinds.PasswordLeaked)];
         IAlert[] securityKind =
         [
            .. alerts.Where(x => x.Kind is AlertKinds.VaultSecuritySettings
               or AlertKinds.HostSecuritySettings),
         ];
         IAlert[] passkeyKind =
         [
            .. alerts.Where(x => x.Kind is AlertKinds.InsufficientPasskeys
               or AlertKinds.WeakPasskey
               or AlertKinds.PasskeyLeaked
               or AlertKinds.PasskeyReusedAsAccountPassword),
         ];

         int activityAlerts = activityKind
            .OfType<IActivityReviewAlert>()
            .SelectMany(x => x.Activities)
            .Count();
         int expiredPasswordAlerts = expiredKind
            .OfType<IAccountsAlert>()
            .SelectMany(x => x.Accounts)
            .Count();
         int duplicatedPasswordAlerts = duplicatedKind.Length;
         int leakedPasswordAlerts = leakedKind
            .OfType<IAccountsAlert>()
            .SelectMany(x => x.Accounts)
            .Count();
         int securitySettingsAlerts = securityKind
            .OfType<IVaultSecuritySettingsAlert>()
            .Sum(x => BitOperations.PopCount((uint)x.Issues))
            + securityKind
            .OfType<IHostSecuritySettingsAlert>()
            .Sum(x => BitOperations.PopCount((uint)x.Issues));
         int passkeyQualityAlerts = _passkeyQualityCount(passkeyKind);

         int totalAlertCount = activityAlerts
            + expiredPasswordAlerts
            + duplicatedPasswordAlerts
            + leakedPasswordAlerts
            + securitySettingsAlerts
            + passkeyQualityAlerts;

         AlertSeverity activitySeverity = AlertBroker.MaxSeverity(activityKind);
         AlertSeverity expiredSeverity = AlertBroker.MaxSeverity(expiredKind);
         AlertSeverity duplicatedSeverity = AlertBroker.MaxSeverity(duplicatedKind);
         AlertSeverity leakedSeverity = AlertBroker.MaxSeverity(leakedKind);
         AlertSeverity securitySeverity = AlertBroker.MaxSeverity(securityKind);
         AlertSeverity passkeySeverity = AlertBroker.MaxSeverity(passkeyKind);

         _viewModel.ShowAlerts = Strings.Format(nameof(Strings.Msg_ShowAlerts), totalAlertCount);
         _viewModel.ShowAlertsColor = AlertBroker.BrushFor(alerts);
         _viewModel.ShowActivityAlertsColor = AlertBroker.BrushFor(activitySeverity);
         _viewModel.ShowExpiredPasswordAlertsColor = AlertBroker.BrushFor(expiredSeverity);
         _viewModel.ShowDuplicatedPasswordAlertsColor = AlertBroker.BrushFor(duplicatedSeverity);
         _viewModel.ShowLeakedPasswordAlertsColor = AlertBroker.BrushFor(leakedSeverity);
         _viewModel.ShowSecuritySettingsAlertsColor = AlertBroker.BrushFor(securitySeverity);
         _viewModel.ShowPasskeyQualityAlertsColor = AlertBroker.BrushFor(passkeySeverity);
         _viewModel.ShowActivityAlerts = Strings.Format(nameof(Strings.Msg_ShowActivityAlerts), activityAlerts);
         _viewModel.ShowExpiredPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowExpiredPasswordAlerts), expiredPasswordAlerts);
         _viewModel.ShowDuplicatedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowDuplicatedPasswordAlerts), duplicatedPasswordAlerts);
         _viewModel.ShowLeakedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowLeakedPasswordAlerts), leakedPasswordAlerts);
         _viewModel.ShowSecuritySettingsAlerts = Strings.Format(nameof(Strings.Msg_ShowSecuritySettingsAlerts), securitySettingsAlerts);
         _viewModel.ShowPasskeyQualityAlerts = Strings.Format(nameof(Strings.Msg_ShowPasskeyQualityAlerts), passkeyQualityAlerts);

         _alerts_MI.Visibility = totalAlertCount != 0 ? Visibility.Visible : Visibility.Collapsed;
         _activityAlerts_MI.Visibility = activityAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;
         _expiredPasswordAlerts_MI.Visibility = expiredPasswordAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;
         _duplicatedPasswordAlerts_MI.Visibility = duplicatedPasswordAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;
         _leakedPasswordAlerts_MI.Visibility = leakedPasswordAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;
         _securitySettingsAlerts_MI.Visibility = securitySettingsAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;
         _passkeyQualityAlerts_MI.Visibility = passkeyQualityAlerts != 0 ? Visibility.Visible : Visibility.Collapsed;

         _reorderAlertsMenu(
         [
            (_leakedPasswordAlerts_MI, leakedSeverity, leakedPasswordAlerts),
            (_passkeyQualityAlerts_MI, passkeySeverity, passkeyQualityAlerts),
            (_expiredPasswordAlerts_MI, expiredSeverity, expiredPasswordAlerts),
            (_activityAlerts_MI, activitySeverity, activityAlerts),
            (_duplicatedPasswordAlerts_MI, duplicatedSeverity, duplicatedPasswordAlerts),
            (_securitySettingsAlerts_MI, securitySeverity, securitySettingsAlerts),
         ]);
      }

      /// <summary>
      /// Critical first, then Warning, then Info. Hidden items stay at the end;
      /// equal severity keeps the stable secondary order of <paramref name="entries"/>.
      /// </summary>
      private void _reorderAlertsMenu(
         (MenuItem Item, AlertSeverity Severity, int Count)[] entries)
      {
         MenuItem[] ordered =
         [
            .. entries
               .Where(static e => e.Count > 0)
               .OrderByDescending(static e => e.Severity)
               .Select(static e => e.Item),
            .. entries
               .Where(static e => e.Count == 0)
               .Select(static e => e.Item),
         ];

         _alerts_MI.Items.Clear();
         foreach (MenuItem item in ordered)
         {
            _ = _alerts_MI.Items.Add(item);
         }
      }

      private static int _passkeyQualityCount(IEnumerable<IAlert> alerts)
      {
         int count = 0;

         foreach (IAlert alert in alerts)
         {
            switch (alert)
            {
               case IInsufficientPasskeysAlert:
                  count++;
                  break;
               case IWeakPasskeyAlert weak:
                  count++;
                  break;
               case IPasskeyLeakedAlert leaked:
                  count++;
                  break;
               case IPasskeyReuseAlert reuse:
                  count++;
                  break;
            }
         }

         return count;
      }

      private void _clearFilter()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _viewModel.ClearFilters();
      }

      private void _focusServiceFilter()
      {
         _serviceFilter_TB.SelectAll();
         _ = _serviceFilter_TB.Focus();
      }

      private void _openUserSettings()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         UserSettingsView.ShowUserSettings(this);
         _viewModel.RefreshFilters();
      }

      private void _navigation_ItemRequested(object? sender, string itemId)
      {
         if (_session.Database?.User is null)
         {
            return;
         }

         if (string.IsNullOrEmpty(itemId)
            || _session.Database.User.ItemId == itemId)
         {
            _openUserSettings();
            return;
         }

         _ = Activate();

         _clearFilter();

         switch (itemId[0])
         {
            case 'S':
               _viewModel.SelectedService = _viewModel.Services.FirstOrDefault(x => x.Service.ItemId == itemId);
               break;
            case 'A':
               _viewModel.SelectedService = _viewModel.Services.FirstOrDefault(x => x.Service.Accounts.Any(y => y.ItemId == itemId));
               if (!_service_SV.SelectAccount(itemId))
               {
                  _viewModel.SelectedService = null;
               }
               break;
            default:
               break;
         }

         if (_viewModel.SelectedService is not null)
         {
            _services_LB.ScrollIntoView(_viewModel.SelectedService);
         }
         else
         {
            _dialogs.Warn(Strings.Format(nameof(Strings.Msg_ItemNotFound), itemId), Strings.Title_ItemNotFound);
         }
      }

      public void Dispose()
      {
         _dispose(true);
         GC.SuppressFinalize(this);
      }

      private void _dispose(bool disposing)
      {
         if (disposing)
         {
            _viewModel.Dispose();
         }
      }

      private void _userServicesView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
      {
         string sourceText = (e.OriginalSource as TextBlock)?.Text ?? string.Empty;

         if (sourceText != _userServices_GB.Header.ToString())
         {
            return;
         }

         string? itemId = AppServices.Session.User?.ItemId;

         if (itemId is null)
         {
            return;
         }

         AppServices.Clipboard.SetText(itemId);

         e.Handled = true;
      }
   }
}
