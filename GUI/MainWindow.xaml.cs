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
      public MainWindow()
      {
         InitializeComponent();

         DataContext = new MainViewModel();

         this.Loaded += _mainWindow_Loaded;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);

         /// TODO : To be removed
         var pwd = PasswordGenerator.ShowGeneratePasswordDialog(this);
      }

      private void _generatePassword_MenuItem_Click(object sender, RoutedEventArgs e)
      {
         new PasswordGenerator
         {
            Owner = this
         }
         .ShowDialog();
      }
   }
}