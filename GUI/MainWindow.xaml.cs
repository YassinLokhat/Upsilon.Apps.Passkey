using System.Windows;
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

      public MainWindow()
      {
         InitializeComponent();

         DataContext = _mainViewModel = new MainViewModel();

         this.Loaded += _mainWindow_Loaded;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);

         /// TODO : To be removed
         //_newUser_MenuItem_Click(this, e);
      }

      private void _newUser_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         new UserView(database: null)
         {
            Owner = this
         }
         .ShowDialog();
      }

      private void _generatePassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         new PasswordGenerator
         {
            Owner = this
         }
         .ShowDialog();
      }

      private void _usernameTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            _passwordVisiblePasswordBox_Validated(this, EventArgs.Empty);
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            _passwordVisiblePasswordBox_Aborded(this, EventArgs.Empty);
         }
      }

      private void _passwordVisiblePasswordBox_Validated(object sender, EventArgs e)
      {
         _mainViewModel.Label = "Password :";
      }

      private void _passwordVisiblePasswordBox_Aborded(object sender, EventArgs e)
      {
         _mainViewModel.Label = "Username :";
      }
   }
}