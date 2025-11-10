using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
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
      private readonly DispatcherTimer _timer;
      private readonly string _title;
      private ObservableCollection<ServiceViewModel> _services;

      private UserServicesView()
      {
         InitializeComponent();

         if (MainViewModel.Database is null) throw new NullReferenceException(nameof(MainViewModel.Database));

         _timer = new()
         {
            Interval = new TimeSpan(0, 0, 1),
            IsEnabled = true,
         };

         Title = _title = $"{MainViewModel.AppTitle} - User '{MainViewModel.User.Username}'";

         _services = [.. MainViewModel.User.Services.OrderBy(x => x.ServiceName).Select(x => new ServiceViewModel(x))];
         _services_LB.ItemsSource = _services;

         MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         _timer.Tick += _timer_Elapsed;
         Loaded += _userServicesView_Loaded;
      }

      private void _database_DatabaseClosed(object? sender, PassKey.Core.Public.Events.LogoutEventArgs e)
      {
         _timer.Stop();

         try
         {
            Dispatcher.Invoke(() =>
            {
               DialogResult = true;
            });
         }
         catch { }
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         string title = _title;

         if (MainViewModel.Database is not null)
         {
            if (MainViewModel.Database.HasChanged())
            {
               title += " - *";
            }

            int sessionLeftTime = MainViewModel.Database.SessionLeftTime ?? 0;
            title += $" - Left session time : {sessionLeftTime / 60:D2}:{sessionLeftTime % 60:D2}";
         }

         Title = title;
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
         _service_SV.DataContext = (ServiceViewModel)_services_LB.SelectedItem;
      }

      private void _save_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         IsEnabled = false;

         Task.Run(() => 
         {
            MainViewModel.Database?.Save();

            Dispatcher.Invoke(() =>
            {
               IsEnabled = true;
            });
         });
      }
   }
}
