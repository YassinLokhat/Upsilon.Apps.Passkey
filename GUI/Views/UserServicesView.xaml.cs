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

      private string[] _services = ["S1", "S2"];

      private UserServicesView()
      {
         InitializeComponent();

         _timer = new()
         {
            Interval = new TimeSpan(0, 0, 1),
            IsEnabled = true,
         };

         Title = _title = $"{MainViewModel.AppTitle} - User '{MainViewModel.User.Username}'";

         _services_LB.ItemsSource = _services;

         _timer.Tick += _timer_Elapsed;
         Loaded += _userServicesView_Loaded;
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
   }
}
