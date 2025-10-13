using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PasswordsContainer.xaml
   /// </summary>
   public partial class PasswordsContainer : UserControl
   {
      private IDatabase? _database;
      private readonly StackPanel _stackPanel;
      private readonly List<PasswordItem> _passwords;

      public PasswordsContainer(IDatabase? database)
      {
         InitializeComponent();

         _database = database;
         _passwords = [];
         _stackPanel = (StackPanel)FindName("Passwords");
         
         if (database != null
            && database.User != null)
         {
            foreach (var password in database.User.Passkeys)
            {
               _addPassword(password);
            }
         }
         else
         {
            _addPassword(string.Empty);
         }
      }

      private void _passwordItem_DeleteClicked(object? sender, EventArgs e)
      {
         if (sender is not PasswordItem passwordItem) return;
      }

      private void _passwordItem_DownClicked(object? sender, EventArgs e)
      {
         throw new NotImplementedException();
      }

      private void _passwordItem_UpClicked(object? sender, EventArgs e)
      {
         throw new NotImplementedException();
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _addPassword(string.Empty);
      }

      private void _addPassword(string password)
      {
         PasswordItem passwordItem = new(new() { Index = _passwords.Count, Password = password });
         passwordItem.UpClicked += _passwordItem_UpClicked;
         passwordItem.DownClicked += _passwordItem_DownClicked;
         passwordItem.DeleteClicked += _passwordItem_DeleteClicked;

         _passwords.Add(passwordItem);
         _stackPanel.Children.Add(passwordItem);
      }

      private void _movePassword(int oldIndex, int newIndex, PasswordItem passwordItem)
      {

      }
   }
}
