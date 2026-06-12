using System.Windows;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for UserServicesView.xaml
   /// </summary>
   public partial class UserServicesView : Window
   {
      private readonly UserServicesViewModel _viewModel;
      private int _autoLoginHotkeyId = 0;
      private int _autoPasswordHotkeyId = 0;
      private Task? _saveTask;

      private static ISessionService Session => AppServices.Session;
      private static IDialogService Dialogs => AppServices.Dialogs;
      private static INavigationService Navigation => AppServices.Navigation;

      private UserServicesView()
      {
         InitializeComponent();

         IDatabase database = Session.Database
            ?? throw new InvalidOperationException("UserServicesView requires an active session.");

         Navigation.ItemRequested += _navigation_ItemRequested;

         DataContext = _viewModel = new($"{AppInfo.Title} - '{Session.User}'");
         _viewModel.FiltersRefreshed += _viewModel_FiltersRefreshed;

         _services_LB.ItemsSource = _viewModel.Services;

         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }

         _warnings_MI.Visibility = Visibility.Collapsed;

         _ = _serviceFilter_TB.Focus();

         database.DatabaseClosed += _database_DatabaseClosed;
         database.WarningsUpdated += _database_WarningUpdated;
         Loaded += _userServicesView_Loaded;
      }

      private void _database_WarningUpdated(object? sender, Interfaces.Events.WarningsUpdatedEventArgs e)
      {
         _ = Dispatcher.BeginInvoke(() => { _updateWarningsMenu(e.Warnings); });
      }

      private void _viewModel_FiltersRefreshed(object? sender, EventArgs e)
      {
         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }
      }

      private void _database_DatabaseClosed(object? sender, Interfaces.Events.LogoutEventArgs e)
      {
         if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
         {
            return;
         }

         _ = Dispatcher.BeginInvoke(() =>
         {
            DialogResult = true;
         });
      }

      public static bool ShowUser(Window owner)
      {
         return new UserServicesView()
         {
            Owner = owner,
         }
         .ShowDialog()
         ?? true;
      }

      private void _userServicesView_Loaded(object sender, RoutedEventArgs e)
      {
         _autoLoginHotkeyId = HotkeyHelper.Register(this, ModifierKeys.Control | ModifierKeys.Shift, Key.L);
         _autoPasswordHotkeyId = HotkeyHelper.Register(this, ModifierKeys.Control | ModifierKeys.Shift, Key.P);

         HotkeyHelper.HotkeyPressed += _hotkeyHelper_HotkeyPressed;

         this.PostLoadSetup();
      }

      private void _hotkeyHelper_HotkeyPressed(object? sender, HotkeyEventArgs e)
      {
         if (this.GetIsBusy()) return;

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
            QrCodeView.CopyToClipboard(toInsert);
            HotkeyHelper.Send(ModifierKeys.Control, Key.V);
         }
      }

      private void _userSettings_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         _openSettings();
      }

      private void _openSettings()
      {
         if (this.GetIsBusy()) return;

         UserSettingsView.ShowUserSettings(this);
         _viewModel.RefreshFilters();
      }

      private void _generateRandomPassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         string? password = PasswordGenerator.ShowGeneratePasswordDialog(this);

         if (password is null) return;

         _service_SV.SetSelectedPassword(password);
      }

      private void _logout_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         DialogResult = true;
      }

      private void _window_Closed(object sender, EventArgs e)
      {
         _ = HotkeyHelper.Unregister(this, _autoLoginHotkeyId);
         _ = HotkeyHelper.Unregister(this, _autoPasswordHotkeyId);
         HotkeyHelper.HotkeyPressed -= _hotkeyHelper_HotkeyPressed;

         Navigation.ItemRequested -= _navigation_ItemRequested;

         Dialogs.Close<AccountPasswordsWarningView>();
         Dialogs.Close<DuplicatedPasswordsWarningView>();
         Dialogs.Close<UserActivitiesView>();

         Session.EndSession();

         _viewModel.Dispose();
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         Session.User?.Shake();
         _service_SV.SetDataContext((ServiceViewModel)_services_LB.SelectedItem);
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         string? serviceId = _service_SV.GetServiceId();
         string? accountId = _service_SV.GetAccountId();
         this.SetIsBusy(true);

         if (_saveTask is not null
            && !_saveTask.IsCompleted)
         {
            return;
         }

         _saveTask = Task.Run(() =>
         {
            Session.Database?.Save();

            _ = Dispatcher.BeginInvoke(() =>
            {
               _viewModel.RefreshFilters();
               ServiceViewModel? service = _viewModel.Services.FirstOrDefault(x => x.Service.ItemId == serviceId);

               this.SetIsBusy(false);

               _services_LB.ItemsSource = _viewModel.Services;
               _services_LB.SelectedItem = service;

               if (!string.IsNullOrEmpty(accountId))
               {
                  _ = _service_SV.SelectAccount(accountId);
               }
            });
         });
      }

      private void _updateWarningsMenu(IWarning[] warnings)
      {
         int totalWarningCount = 0;
         int activityWarnings = 0;
         int expiredPasswordWarnings = 0;
         int duplicatedPasswordWarnings = 0;
         int leakedPasswordWarnings = 0;

         if (Session.Database?.Warnings is not null)
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

            totalWarningCount = activityWarnings + expiredPasswordWarnings + duplicatedPasswordWarnings + leakedPasswordWarnings;
            _viewModel.ShowWarnings = $"Show {totalWarningCount} warnings";
            _viewModel.ShowWarningsColor = (expiredPasswordWarnings + leakedPasswordWarnings) == 0 ? SemanticBrushes.Warning : SemanticBrushes.Danger;
            _viewModel.ShowActivityWarnings = $"Show {activityWarnings} activities to review";
            _viewModel.ShowExpiredPasswordWarnings = $"Show {expiredPasswordWarnings} expired passwords";
            _viewModel.ShowDuplicatedPasswordWarnings = $"Show {duplicatedPasswordWarnings} duplicated passwords";
            _viewModel.ShowLeakedPasswordWarnings = $"Show {leakedPasswordWarnings} leaked passwords";
         }

         _warnings_MI.Visibility = totalWarningCount != 0 ? Visibility.Visible : Visibility.Collapsed;
         _activityWarnings_MI.Visibility = activityWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _expiredPasswordWarnings_MI.Visibility = expiredPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _duplicatedPasswordWarnings_MI.Visibility = duplicatedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
         _leakedPasswordWarnings_MI.Visibility = leakedPasswordWarnings != 0 ? Visibility.Visible : Visibility.Collapsed;
      }

      private void _addService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         _services_LB.SelectedItem = _viewModel.AddService();
      }

      private void _deleteService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         if (_services_LB.SelectedItem is not ServiceViewModel serviceViewModel
            || Dialogs.Confirm($"Are you sure you want to delete the service '{serviceViewModel.ServiceDisplay}'", "Delete Service") != MessageBoxResult.Yes)
         {
            return;
         }

         _services_LB.SelectedIndex = _viewModel.DeleteService(serviceViewModel);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _clearFilter();
      }

      private void _clearFilter()
      {
         if (this.GetIsBusy()) return;

         _viewModel.ServiceFilter = _viewModel.TextFilter = _viewModel.IdentifierFilter = string.Empty;
         _viewModel.ChangedItemsOnly = false;
      }

      private void _showActivities_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         _ = Dialogs.ShowSingleton(
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
         if (Session.Database?.User is null) return;

         if (string.IsNullOrEmpty(itemId)
            || Session.Database.User.ItemId == itemId)
         {
            _openSettings();
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
            Dialogs.Warn($"The item '{itemId}' was not found.\nIt has been deleted.", "Item not found");
         }
      }

      private void _activityWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         _ = Dialogs.ShowSingleton(
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
         if (this.GetIsBusy()) return;

         _ = Dialogs.ShowSingleton(() => new DuplicatedPasswordsWarningView());
      }

      private void _expiredOrLeakedPasswordWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         WarningType requested = sender == _expiredPasswordWarnings_MI
            ? WarningType.PasswordUpdateReminderWarning
            : WarningType.PasswordLeakedWarning;

         _ = Dialogs.ShowSingleton(
            factory: () => new AccountPasswordsWarningView(requested),
            configure: view =>
            {
               if (view.DataContext is AccountPasswordsWarningViewModel vm)
               {
                  vm.WarningType = requested;
               }
            });
      }

      private void _filterCommand_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
      {
         _serviceFilter_TB.SelectAll();
         _ = _serviceFilter_TB.Focus();
      }
   }
}
