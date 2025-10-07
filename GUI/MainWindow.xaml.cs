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
      public static string AppTitle
      {
         get
         {
            var package = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var packageVersion = package.Version?.ToString(2);

            return $"{package.Name} v{packageVersion}";
         }
      }

      public MainWindow()
      {
         InitializeComponent();

         DataContext = new TitleViewModel();

         this.Loaded += _mainWindow_Loaded;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
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