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
         _viewModel.LanguageRefreshed += (_, _) => _refreshWarningsMenuFromSession();
         _viewModel.ThemeRefreshed += (_, _) => _refreshWarningsMenuFromSession();
         _viewModel.SaveRequested += (_, _) => _save();
         _viewModel.UserSettingsRequested += (_, _) => _openUserSettings();
         _viewModel.GeneratePasswordRequested += (_, _) => _generateRandomPassword();
         _viewModel.ShowActivitiesRequested += (_, _) => _showActivities();
         _viewModel.AppSettingsRequested += (_, _) => _openAppSettings();
         _viewModel.FocusFilterRequested += (_, _) => _focusServiceFilter();

         _services_LB.ItemsSource = _viewModel.Services;

         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }

         _warnings_MI.Visibility = Visibility.Collapsed;

         _ = _serviceFilter_TB.Focus();

         _database.DatabaseClosed += _database_DatabaseClosed;
         _database.WarningsUpdated += _database_WarningUpdated;
         Loaded += _userServicesView_Loaded;

         if (_database.Warnings is not null
            && _database.Warnings.Any())
         {
            _database_WarningUpdated(_notifiedWarnings());
         }
      }

      private void _database_WarningUpdated(object? sender, Interfaces.Events.WarningsUpdatedEventArgs e)
      {
         _database_WarningUpdated(e.Warnings);
      }

      private void _database_WarningUpdated(IEnumerable<IWarning> warnings)
      {
         _ = Dispatcher.BeginInvoke(() => { _updateWarningsMenu([.. warnings]); });
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

         if (_database.User?.Settings.WarningsToNotify == 0)
         {
            _dialogs.Warn(Strings.Msg_NoWarningsToNotify, Strings.Title_NoWarningsToNotify);
         }
      }

      // TODO : Map the followin code to new commands in the view and view model
      /*private void _hotkeyHelper_HotkeyPressed(object? sender, HotkeyEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         string? toInsert = null;

         switch (e.Key)
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
            HotkeyHelper.Send(ModifierKeys.Control, Key.V);
         }
      }*/

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
         _database.WarningsUpdated -= _database_WarningUpdated;

         _navigation.ItemRequested -= _navigation_ItemRequested;

         _dialogs.Close<AccountPasswordsWarningView>();
         _dialogs.Close<DuplicatedPasswordsWarningView>();
         _dialogs.Close<SecuritySettingsWarningView>();
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

      private void _refreshWarningsMenuFromSession()
         => _updateWarningsMenu(_notifiedWarnings());

      /// <summary>
      /// <see cref="IDatabase.Warnings"/> holds every computed warning;
      /// <see cref="ISettings.WarningsToNotify"/> decides which ones the menu shows.
      /// </summary>
      private IWarning[] _notifiedWarnings()
      {
         WarningType mask = _database.User?.Settings.WarningsToNotify ?? 0;
         return mask == 0 ? [] : [.. (_database.Warnings ?? []).Where(w => mask.HasFlag(w.WarningType))];
      }

      private void _updateWarningsMenu(IWarning[] warnings)
      {
         int totalWarningCount = 0;
         int activityWarnings = 0;
         int expiredPasswordWarnings = 0;
         int duplicatedPasswordWarnings = 0;
         int leakedPasswordWarnings = 0;
         int securitySettingsWarnings = 0;
         int loginWarnings = 0;

         if (_session.Database?.Warnings is not null)
         {
            activityWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.ActivityReviewWarning))
               .SelectMany(x => x.Activities ?? [])
               .Count();
            expiredPasswordWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.PasswordUpdateReminderWarning))
               .SelectMany(x => x.Accounts ?? [])
               .Count();
            duplicatedPasswordWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.DuplicatedPasswordsWarning))
               .Count();
            leakedPasswordWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.PasswordLeakedWarning))
               .SelectMany(x => x.Accounts ?? [])
               .Count();
            securitySettingsWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.SecuritySettingsWarning))
               .Sum(x => System.Numerics.BitOperations.PopCount((uint)x.SecuritySettingsIssues));
            loginWarnings = warnings
               .Where(x => x.WarningType.HasFlag(WarningType.ActivityReviewWarning)
                  && x.Activities is not null
                  && x.Activities.Any(y => y.EventType is ActivityEventType.LoginFailed
                     or ActivityEventType.LoginSessionTimeoutReached))
               .SelectMany(x => x.Activities ?? [])
               .Count();

            totalWarningCount = activityWarnings
               + expiredPasswordWarnings
               + duplicatedPasswordWarnings
               + leakedPasswordWarnings
               + securitySettingsWarnings;
            _viewModel.ShowWarnings = Strings.Format(nameof(Strings.Msg_ShowWarnings), totalWarningCount);
            _viewModel.ShowWarningsColor = (expiredPasswordWarnings + leakedPasswordWarnings + loginWarnings) == 0 ? SemanticBrushes.Warning : SemanticBrushes.Danger;
            _viewModel.ShowActivityWarningsColor = loginWarnings == 0 ? SemanticBrushes.Warning : SemanticBrushes.Danger;
            _viewModel.ShowActivityWarnings = Strings.Format(nameof(Strings.Msg_ShowActivityWarnings), activityWarnings);
            _viewModel.ShowExpiredPasswordWarnings = Strings.Format(nameof(Strings.Msg_ShowExpiredPasswordWarnings), expiredPasswordWarnings);
            _viewModel.ShowDuplicatedPasswordWarnings = Strings.Format(nameof(Strings.Msg_ShowDuplicatedPasswordWarnings), duplicatedPasswordWarnings);
            _viewModel.ShowLeakedPasswordWarnings = Strings.Format(nameof(Strings.Msg_ShowLeakedPasswordWarnings), leakedPasswordWarnings);
            _viewModel.ShowSecuritySettingsWarnings = Strings.Format(nameof(Strings.Msg_ShowSecuritySettingsWarnings), securitySettingsWarnings);
         }

         _warnings_MI.Visibility = totalWarningCount != 0 ? Visibility.Visible : Visibility.Collapsed;
         _activityWarnings_MI.Visibility = activityWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _expiredPasswordWarnings_MI.Visibility = expiredPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _duplicatedPasswordWarnings_MI.Visibility = duplicatedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _leakedPasswordWarnings_MI.Visibility = leakedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _securitySettingsWarnings_MI.Visibility = securitySettingsWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
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

      private void _activityWarnings_MI_Click(object sender, RoutedEventArgs e)
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

      private void _duplicatedPasswordWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(() => new DuplicatedPasswordsWarningView());
      }

      private void _expiredOrLeakedPasswordWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         WarningType requested = sender.Equals(_expiredPasswordWarnings_MI)
            ? WarningType.PasswordUpdateReminderWarning
            : WarningType.PasswordLeakedWarning;

         _ = _dialogs.ShowSingleton(
            factory: () => new AccountPasswordsWarningView(requested),
            configure: view =>
            {
               if (view.DataContext is AccountPasswordsWarningViewModel vm)
               {
                  vm.WarningType = requested;
               }
            });
      }

      private void _securitySettingsWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         _ = _dialogs.ShowSingleton(() => new SecuritySettingsWarningView());
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
