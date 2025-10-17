using System.Windows;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.Views;

namespace Upsilon.Apps.Passkey.GUI
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private readonly MainViewModel _mainViewModel;
      private readonly DispatcherTimer _timer;

      public MainWindow()
      {
         InitializeComponent();

         DataContext = _mainViewModel = new MainViewModel();

         _timer = new DispatcherTimer()
         {
            Interval = new TimeSpan(0, 0, 5),
         };

         _username_TB.KeyUp += _credential_TB_KeyUp;
         _password_PB.KeyUp += _credential_TB_KeyUp;
         _timer.Tick += _timer_Elapsed;
         Loaded += _mainWindow_Loaded;
      }

      private void _timer_Elapsed(object? sender, EventArgs e)
      {
         _resetCredentials();
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);

         /// TODO : To be removed
         _newUser_MenuItem_Click(this, e);
      }

      private void _newUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         _ = new UserView()
         {
            Owner = this
         }
         .ShowDialog();
      }

      private void _generatePassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         _ = new PasswordGenerator
         {
            Owner = this
         }
         .ShowDialog();
      }

      private void _credential_TB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            if (sender == _username_TB)
            {
               if (string.IsNullOrEmpty(_username_TB.Text)) return;

               _mainViewModel.Label = "Password :";

               _username_TB.Text = string.Empty;
               _username_TB.Visibility = Visibility.Hidden;

               _password_PB.Password = string.Empty;
               _password_PB.Visibility = Visibility.Visible;
               _ = _password_PB.Focus();
            }
            else
            {
               if (string.IsNullOrEmpty(_password_PB.Password)) return;
            }

            _password_PB.Password = string.Empty;
            _timer.Stop();
            _timer.Start();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            _resetCredentials();
         }
      }

      private void _resetCredentials()
      {
         _mainViewModel.Label = "Username :";

         _username_TB.Text = string.Empty;
         _username_TB.Visibility = Visibility.Visible;
         _ = _username_TB.Focus();

         _password_PB.Password = string.Empty;
         _password_PB.Visibility = Visibility.Hidden;

         _timer.Stop();
      }
   }
}