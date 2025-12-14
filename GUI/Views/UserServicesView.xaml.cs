using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

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

      private void _database_DatabaseClosed(object? sender, Passkey.Interfaces.Events.LogoutEventArgs e)
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
         UserSettingsView.ShowUserSettings(this);
         _viewModel.RefreshFilters();
      }

      private void _generateRandomPassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         string? password = PasswordGenerator.ShowGeneratePasswordDialog(this);

         if (password is null) return;

         _service_SV.SetSelectedPassword(password);
      }

      private void _logout_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
      }

      private void _window_Closed(object sender, EventArgs e)
      {
         HotkeyHelper.Unregister(this, _autoLoginHotkeyId);
         HotkeyHelper.Unregister(this, _autoPasswordHotkeyId);

         if (MainViewModel.Database is null || MainViewModel.Database.User is null) return;

         MainViewModel.Database?.Close();
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         MainViewModel.User.Shake();
         _service_SV.SetDataContext((ServiceViewModel)_services_LB.SelectedItem);
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         string? serviceId = ((ServiceViewModel?)_services_LB.SelectedItem)?.ServiceId;
         Cursor = Cursors.Wait;

         _ = Task.Run(() =>
         {
            MainViewModel.Database?.Save();

            Dispatcher.Invoke(() =>
            {
               _viewModel.RefreshFilters();
               ServiceViewModel? service = _viewModel.Services.FirstOrDefault(x => x.ServiceId == serviceId);

               _services_LB.ItemsSource = _viewModel.Services;
               _services_LB.SelectedItem = service;

               Cursor = Cursors.Arrow;
            });
         });
      }

      private void _addService_Button_Click(object sender, RoutedEventArgs e)
      {
         _services_LB.SelectedItem = _viewModel.AddService();
      }

      private void _deleteService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (_services_LB.SelectedItem is not ServiceViewModel serviceViewModel
            || MessageBox.Show($"Are you sure you want to delete the service '{serviceViewModel.ServiceDisplay}'", "Delete Service", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
         {
            return;
         }

         _services_LB.SelectedIndex = _viewModel.DeleteService(serviceViewModel);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.ServiceFilter = _viewModel.TextFilter = _viewModel.IdentifierFilter = string.Empty;
      }
   }
}
