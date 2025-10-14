using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.Views.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for UserView.xaml
   /// </summary>
   public partial class UserView : Window
   {
      private readonly IDatabase? _database;
      private readonly StackPanel _credentials;
      private readonly UserViewModel _viewModel;

      public UserView(IDatabase? database)
      {
         InitializeComponent();

         _database = database;
         _credentials = (StackPanel)FindName("Credentials");
         _credentials.Children.Add(new PasswordsContainer(_database?.User?.Passkeys));

         DataContext = _viewModel = new UserViewModel(_database is null || _database.User is null);

         Loaded += _mainWindow_Loaded;
      }

      private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         DarkMode.SetDarkMode(this);
      }

      private void _numericUpDown_ValueChanged(object sender, EventArgs e)
      {
         NumericUpDown numericUpDown = (NumericUpDown)sender;

         try
         {
            if (_viewModel.LogoutTimeout != numericUpDown.Value)
               _viewModel.LogoutTimeout = numericUpDown.Value;
         }
         catch { }
      }
   }
}
