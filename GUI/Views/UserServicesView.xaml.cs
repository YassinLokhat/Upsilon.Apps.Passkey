using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserServicesView.xaml
   /// </summary>
   public partial class UserServicesView : Window
   {
      private readonly UserServicesViewModel _viewModel;

      private UserServicesView()
      {
         InitializeComponent();

         if (MainViewModel.Database is null) throw new NullReferenceException(nameof(MainViewModel.Database));

         DataContext = _viewModel = new($"{MainViewModel.AppTitle} - '{MainViewModel.User}'");

         _services_LB.ItemsSource = _viewModel.Services;

         if (_viewModel.Services.Count != 0)
         {
            _services_LB.SelectedIndex = 0;
         }

         MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         Loaded += _userServicesView_Loaded;
      }

      private void _database_DatabaseClosed(object? sender, PassKey.Core.Public.Events.LogoutEventArgs e)
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
         DarkMode.SetDarkMode(this);
      }

      private void _userSettings_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         UserSettingsView.ShowUserSettings(this);
      }

      private void _generateRandomPassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         _ = PasswordGenerator.ShowGeneratePasswordDialog(this);
      }

      private void _logout_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
      }

      private void _window_Closed(object sender, EventArgs e)
      {
         if (MainViewModel.Database is null || MainViewModel.Database.User is null) return;

         MainViewModel.Database?.Close();
      }

      private void _services_LB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         _ = MainViewModel.User.ItemId;
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
         ServiceViewModel? serviceModel = _viewModel.Services.FirstOrDefault(x => x.ServiceName == "New Service");

         if (serviceModel == null)
         {
            IService service = MainViewModel.User.AddService("New Service");
            service.AddAccount(["NewAccount"]);

            serviceModel = new(service);
            _viewModel.Services.Insert(0, serviceModel);
         }

         _services_LB.SelectedItem = serviceModel;
      }

      private void _deleteService_Button_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.Services.Count == 1
            || _services_LB.SelectedItem is not ServiceViewModel serviceModel
            || MessageBox.Show($"Are you sure you want to delete the service '{serviceModel.ServiceName}'", "Delete Service", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
         {
            return;
         }

         _viewModel.Services.Remove(serviceModel);
         MainViewModel.User.DeleteService(serviceModel.Service);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.ServiceFilter = _viewModel.TextFilter = _viewModel.IdentifiantFilter = string.Empty;
      }
   }
}
