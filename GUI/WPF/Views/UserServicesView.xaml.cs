using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.Utils;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Alerts;
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
         _viewModel.UserSettingsRequested += (_, _) => _openUserSettings();
         _viewModel.GeneratePasswordRequested += (_, _) => _generateRandomPassword();
         _viewModel.ShowActivitiesRequested += (_, _) => _showActivities();
         _viewModel.AppSettingsRequested += (_, _) => _openAppSettings();
         _viewModel.FocusFilterRequested += (_, _) => _focusServiceFilter();
         _viewModel.CopyIdentifierRequested += (_, _) => _copyIdentifierOrPassword(Key.L);
         _viewModel.CopyPasswordRequested += (_, _) => _copyIdentifierOrPassword(Key.P);

         _services_LB.ItemsSource = _viewModel.Services;

         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
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
         _ = Dispatcher.BeginInvoke(() => { _updateAlertsMenu(_notifiedAlerts()); });
      }

      private void _viewModel_FiltersRefreshed(object? sender, EventArgs e)
      {
         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
          => this.DatabaseClosed(_isClosing);

      public static bool ShowUser(Window owner)
      {
         using UserServicesView view = new()
         {
            Owner = owner,
         };

         // Only an explicit DialogResult (Logout menu or session-timeout via
         // WindowHelper) should keep the login window open. Closing with X /
         // Alt+F4 leaves DialogResult null — that must exit the app, not
         // return to MainWindow.
         return view.ShowDialog() == true;
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

      private void _openUserSettings()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         UserSettingsView.ShowUserSettings(this);
         _viewModel.RefreshFilters();
      }

      private void _openAppSettings()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         AppSettingsView.ShowAppSettings(this);
         _viewModel.RefreshFilters();
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

      private void _logout_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
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

         _session.EndSession();

         _viewModel.Dispose();
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _session.User?.Shake();
         _service_SV.SetDataContext(_services_LB.SelectedItem as ServiceViewModel);
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
         ServiceViewModel? service = _viewModel.Services.FirstOrDefault(x => x.Service.ItemId == serviceId);

         _services_LB.ItemsSource = _viewModel.Services;
         _services_LB.SelectedItem = service;

         if (!string.IsNullOrEmpty(accountId))
         {
            _ = _service_SV.SelectAccount(accountId);
         }
      }

      private void _refreshAlertsMenuFromSession()
         => _updateAlertsMenu(_notifiedAlerts());

      private static IAlert[] _notifiedAlerts()
         => [.. _session.Alerts.GetNotifiedAlerts()];

      private void _updateAlertsMenu(IAlert[] warnings)
      {
         IAlert[] activityKind = [.. warnings.Where(x => x.Kind == AlertKinds.ActivityReview)];
         IAlert[] expiredKind = [.. warnings.Where(x => x.Kind == AlertKinds.PasswordUpdateReminder)];
         IAlert[] duplicatedKind = [.. warnings.Where(x => x.Kind == AlertKinds.DuplicatedPasswords)];
         IAlert[] leakedKind = [.. warnings.Where(x => x.Kind == AlertKinds.PasswordLeaked)];
         IAlert[] securityKind =
         [
            .. warnings.Where(x => x.Kind is AlertKinds.VaultSecuritySettings
               or AlertKinds.HostSecuritySettings),
         ];
         IAlert[] passkeyKind =
         [
            .. warnings.Where(x => x.Kind is AlertKinds.InsufficientPasskeys
               or AlertKinds.WeakPasskey
               or AlertKinds.PasskeyLeaked
               or AlertKinds.PasskeyReusedAsAccountPassword),
         ];

         int activityWarnings = activityKind
            .OfType<IActivityReviewAlert>()
            .SelectMany(x => x.Activities)
            .Count();
         int expiredPasswordWarnings = expiredKind
            .OfType<IAccountsAlert>()
            .SelectMany(x => x.Accounts)
            .Count();
         int duplicatedPasswordWarnings = duplicatedKind.Length;
         int leakedPasswordWarnings = leakedKind
            .OfType<IAccountsAlert>()
            .SelectMany(x => x.Accounts)
            .Count();
         int securitySettingsWarnings = securityKind
            .OfType<IVaultSecuritySettingsAlert>()
            .Sum(x => BitOperations.PopCount((uint)x.Issues))
            + securityKind
            .OfType<IHostSecuritySettingsAlert>()
            .Sum(x => BitOperations.PopCount((uint)x.Issues));
         int passkeyQualityWarnings = _passkeyQualityCount(passkeyKind);

         int totalWarningCount = activityWarnings
            + expiredPasswordWarnings
            + duplicatedPasswordWarnings
            + leakedPasswordWarnings
            + securitySettingsWarnings
            + passkeyQualityWarnings;

         AlertSeverity activitySeverity = AlertBroker.MaxSeverity(activityKind);
         AlertSeverity expiredSeverity = AlertBroker.MaxSeverity(expiredKind);
         AlertSeverity duplicatedSeverity = AlertBroker.MaxSeverity(duplicatedKind);
         AlertSeverity leakedSeverity = AlertBroker.MaxSeverity(leakedKind);
         AlertSeverity securitySeverity = AlertBroker.MaxSeverity(securityKind);
         AlertSeverity passkeySeverity = AlertBroker.MaxSeverity(passkeyKind);

         _viewModel.ShowAlerts = Strings.Format(nameof(Strings.Msg_ShowAlerts), totalWarningCount);
         _viewModel.ShowAlertsColor = AlertBroker.BrushFor(warnings);
         _viewModel.ShowActivityAlertsColor = AlertBroker.BrushFor(activitySeverity);
         _viewModel.ShowExpiredPasswordAlertsColor = AlertBroker.BrushFor(expiredSeverity);
         _viewModel.ShowDuplicatedPasswordAlertsColor = AlertBroker.BrushFor(duplicatedSeverity);
         _viewModel.ShowLeakedPasswordAlertsColor = AlertBroker.BrushFor(leakedSeverity);
         _viewModel.ShowSecuritySettingsAlertsColor = AlertBroker.BrushFor(securitySeverity);
         _viewModel.ShowPasskeyQualityAlertsColor = AlertBroker.BrushFor(passkeySeverity);
         _viewModel.ShowActivityAlerts = Strings.Format(nameof(Strings.Msg_ShowActivityAlerts), activityWarnings);
         _viewModel.ShowExpiredPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowExpiredPasswordAlerts), expiredPasswordWarnings);
         _viewModel.ShowDuplicatedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowDuplicatedPasswordAlerts), duplicatedPasswordWarnings);
         _viewModel.ShowLeakedPasswordAlerts = Strings.Format(nameof(Strings.Msg_ShowLeakedPasswordAlerts), leakedPasswordWarnings);
         _viewModel.ShowSecuritySettingsAlerts = Strings.Format(nameof(Strings.Msg_ShowSecuritySettingsAlerts), securitySettingsWarnings);
         _viewModel.ShowPasskeyQualityAlerts = Strings.Format(nameof(Strings.Msg_ShowPasskeyQualityAlerts), passkeyQualityWarnings);

         _alerts_MI.Visibility = totalWarningCount != 0 ? Visibility.Visible : Visibility.Collapsed;
         _activityAlerts_MI.Visibility = activityWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _expiredPasswordAlerts_MI.Visibility = expiredPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _duplicatedPasswordAlerts_MI.Visibility = duplicatedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _leakedPasswordAlerts_MI.Visibility = leakedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _securitySettingsAlerts_MI.Visibility = securitySettingsWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _passkeyQualityAlerts_MI.Visibility = passkeyQualityWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;

         _reorderAlertsMenu(
         [
            (_leakedPasswordAlerts_MI, leakedSeverity, leakedPasswordWarnings),
            (_passkeyQualityAlerts_MI, passkeySeverity, passkeyQualityWarnings),
            (_expiredPasswordAlerts_MI, expiredSeverity, expiredPasswordWarnings),
            (_activityAlerts_MI, activitySeverity, activityWarnings),
            (_duplicatedPasswordAlerts_MI, duplicatedSeverity, duplicatedPasswordWarnings),
            (_securitySettingsAlerts_MI, securitySeverity, securitySettingsWarnings),
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

      private static int _passkeyQualityCount(IEnumerable<IAlert> warnings)
      {
         int count = 0;

         foreach (IAlert warning in warnings)
         {
            switch (warning)
            {
               case IInsufficientPasskeysAlert:
                  count++;
                  break;
               case IWeakPasskeyAlert weak:
                  count += Math.Max(1, weak.PasskeyIndexes.Count);
                  break;
               case IPasskeyLeakedAlert leaked:
                  count += Math.Max(1, leaked.PasskeyIndexes.Count);
                  break;
               case IPasskeyReuseAlert reuse:
                  count += Math.Max(1, reuse.Accounts.Count());
                  break;
            }
         }

         return count;
      }

      private void _addService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _services_LB.SelectedItem = _viewModel.AddService();
      }

      private void _deleteService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         if (_services_LB.SelectedItem is not ServiceViewModel serviceViewModel
            || _dialogs.Confirm(Strings.Format(nameof(Strings.Msg_DeleteService), serviceViewModel.ServiceDisplay), Strings.Title_DeleteService) != MessageBoxResult.Yes)
         {
            return;
         }

         _services_LB.SelectedIndex = _viewModel.DeleteService(serviceViewModel);
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

      private void _showActivities()
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(
            factory: () => new UserActivitiesView(needsReviewFilter: false),
            configure: view =>
            {
               if (view.DataContext is UserActivitiesViewModel vm)
               {
                  vm.NeedsReview = false;
               }
            });
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
               _services_LB.SelectedItem = _viewModel.Services.FirstOrDefault(x => x.Service.ItemId == itemId);
               break;
            case 'A':
               _services_LB.SelectedItem = _viewModel.Services.FirstOrDefault(x => x.Service.Accounts.Any(y => y.ItemId == itemId));
               if (!_service_SV.SelectAccount(itemId))
               {
                  _services_LB.SelectedItem = null;
               }
               break;
            default:
               break;
         }

         if (_services_LB.SelectedItem is not null)
         {
            _services_LB.ScrollIntoView(_services_LB.SelectedItem);
         }
         else
         {
            _dialogs.Warn(Strings.Format(nameof(Strings.Msg_ItemNotFound), itemId), Strings.Title_ItemNotFound);
         }
      }

      private void _activityAlerts_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(
            factory: () => new UserActivitiesView(needsReviewFilter: true),
            configure: view =>
            {
               if (view.DataContext is UserActivitiesViewModel vm)
               {
                  vm.NeedsReview = true;
               }
            });
      }

      private void _duplicatedPasswordAlerts_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(() => new DuplicatedPasswordsAlertView());
      }

      private void _expiredOrLeakedPasswordAlerts_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         string requested = sender.Equals(_expiredPasswordAlerts_MI)
            ? AlertKinds.PasswordUpdateReminder
            : AlertKinds.PasswordLeaked;

         _ = _dialogs.ShowSingleton(
            factory: () => new AccountPasswordsAlertView(requested),
            configure: view =>
            {
               if (view.DataContext is AccountPasswordsAlertViewModel vm)
               {
                  vm.Kind = requested;
               }
            });
      }

      private void _securitySettingsAlerts_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(() => new SecuritySettingsAlertView());
      }

      private void _passkeyQualityAlerts_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(() => new PasskeyQualityAlertView());
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
