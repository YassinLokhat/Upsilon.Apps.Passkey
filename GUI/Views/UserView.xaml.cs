using System.Windows;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserView.xaml
   /// </summary>
   public partial class UserView : Window
   {
      public UserView(IDatabase? database)
      {
         InitializeComponent();

         DataContext = new UserViewModel(database);

         Loaded += _mainWindow_Loaded;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }
   }
}
