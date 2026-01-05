using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Views
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

      private UserServicesView()
      {
         InitializeComponent();

         if (MainViewModel.Database is null) throw new NullReferenceException(nameof(MainViewModel.Database));

         DataContext = _viewModel = new($"{MainViewModel.AppTitle} - '{MainViewModel.User}'");
         _viewModel.FiltersRefreshed += _viewModel_FiltersRefreshed;

         _services_LB.ItemsSource = _viewModel.Services;

         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }

         _updateWarningsMenu();

         _ = _serviceFilter_TB.Focus();

         MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         Loaded += _userServicesView_Loaded;
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
         try
         {
            Dispatcher.Invoke(() =>
            {
               DialogResult = true;
            });
         }
         catch { }
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

         DarkMode.SetDarkMode(this);
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

         MainViewModel.Database?.Close();
         MainViewModel.Database = null;
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         MainViewModel.User.Shake();
         _service_SV.SetDataContext((ServiceViewModel)_services_LB.SelectedItem);
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         string? serviceId = ((ServiceViewModel?)_services_LB.SelectedItem)?.ServiceId;
         this.SetIsBusy(true);

         if (_saveTask is not null
            && !_saveTask.IsCompleted)
         {
            return;
         }

         _saveTask = Task.Run(() =>
         {
            MainViewModel.Database?.Save();

            Dispatcher.Invoke(() =>
            {
               _viewModel.RefreshFilters();
               ServiceViewModel? service = _viewModel.Services.FirstOrDefault(x => x.ServiceId == serviceId);

               _services_LB.ItemsSource = _viewModel.Services;
               _services_LB.SelectedItem = service;

               _updateWarningsMenu();

               this.SetIsBusy(false);
            });
         });
      }

      private void _updateWarningsMenu()
      {
         int totalWarningCount = 0;
         int activityWarnings = 0;
         int expiredPasswordWarnings = 0;
         int duplicatedPasswordWarnings = 0;
         int leakedPasswordWarnings = 0;

         if (MainViewModel.Database?.Warnings is not null)
         {
            activityWarnings = MainViewModel.Database.Warnings
               .Where(x => x.WarningType.HasFlag(WarningType.ActivityReviewWarning))
               .SelectMany(x => x.Activities ?? [])
               .Count();
            expiredPasswordWarnings = MainViewModel.Database.Warnings
               .Where(x => x.WarningType.HasFlag(WarningType.PasswordUpdateReminderWarning))
               .SelectMany(x => x.Accounts ?? [])
               .Count();
            duplicatedPasswordWarnings = MainViewModel.Database.Warnings
               .Where(x => x.WarningType.HasFlag(WarningType.DuplicatedPasswordsWarning))
               .Count();
            leakedPasswordWarnings = MainViewModel.Database.Warnings
               .Where(x => x.WarningType.HasFlag(WarningType.PasswordLeakedWarning))
               .SelectMany(x => x.Accounts ?? [])
               .Count();

            totalWarningCount = activityWarnings + expiredPasswordWarnings + duplicatedPasswordWarnings + leakedPasswordWarnings;
            _viewModel.ShowWarnings = $"Show {totalWarningCount} warnings";
            _viewModel.ShowWarningsColor = (expiredPasswordWarnings + leakedPasswordWarnings) == 0 ? Brushes.Yellow : Brushes.Red;
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
            || MessageBox.Show($"Are you sure you want to delete the service '{serviceViewModel.ServiceDisplay}'", "Delete Service", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
      }

      private void _showActivities_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         string? itemId = UserActivitiesView.ShowActivitiesDialog(this, needsReviewFilter: false);

         if (itemId is null) return;

         _goToItem(itemId);
      }

      private void _goToItem(IAccount account) => _goToItem(account.ItemId);

      private void _goToItem(string itemId)
      {
         if (MainViewModel.Database?.User is null) return;

         if (string.IsNullOrEmpty(itemId)
            || MainViewModel.Database.User.ItemId == itemId)
         {
            _openSettings();
            return;
         }

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
            _ = MessageBox.Show($"The item '{itemId}' was not found.\nIt has been deleted.", "Item not found", MessageBoxButton.OK, MessageBoxImage.Warning);
         }
      }

      private void _activityWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         string? itemId = UserActivitiesView.ShowActivitiesDialog(this, needsReviewFilter: true);

         if (itemId is null) return;

         _goToItem(itemId);
      }

      private void _duplicatedPasswordWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         IAccount? account = DuplicatedPasswordsWarningView.ShowDuplicatedPaswwordsWarningsDialog(this);

         if (account is null) return;

         _goToItem(account);
      }

      private void _expiredOrLeakedPasswordWarnings_MI_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         IAccount? account = AccountPasswordsWarningView.ShowAccountWarningsDialog(this,
            sender == _expiredPasswordWarnings_MI ? WarningType.PasswordUpdateReminderWarning : WarningType.PasswordLeakedWarning);

         if (account is null) return;

         _goToItem(account);
      }
   }
}
