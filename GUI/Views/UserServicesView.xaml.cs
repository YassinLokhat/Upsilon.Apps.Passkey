using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserServicesView.xaml
   /// </summary>
   public partial class UserServicesView : Window
   {
      private readonly DispatcherTimer _timer;
      private readonly string _title;

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

         _services_LB.ItemsSource = MainViewModel.User.Services;

         MainViewModel.Database.DatabaseClosed += _database_DatabaseClosed;
         _timer.Tick += _timer_Elapsed;
         Loaded += _userServicesView_Loaded;
      }

      private void _database_DatabaseClosed(object? sender, PassKey.Core.Public.Events.LogoutEventArgs e)
      {
         _timer.Stop();
         Dispatcher.Invoke(() =>
         {
            DialogResult = true;
         });
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         Title = $"{_title} - Left session time : {MainViewModel.User.SessionLeftTime / 60:D2}:{MainViewModel.User.SessionLeftTime % 60:D2}";
      }

      public static void ShowUser(Window owner)
      {
         _ = new UserServicesView()
         {
            Owner = owner,
         }
         .ShowDialog();
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
         MainViewModel.Database?.Close();
      }
   }
}
