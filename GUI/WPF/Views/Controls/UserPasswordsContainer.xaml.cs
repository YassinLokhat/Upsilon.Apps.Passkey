using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for UserPasswordsContainer.xaml
   /// </summary>
   public partial class UserPasswordsContainer : UserControl
   {
      public string[] Passkeys => [.. _passwords.Select(x => x.ViewModel.Password)];

      private readonly List<UserPasswordItem> _passwords;

      public UserPasswordsContainer()
      {
         InitializeComponent();

         _passwords = [];

         if (MainViewModel.Database?.User?.Passkeys is not null)
         {
            foreach (string password in MainViewModel.Database.User.Passkeys)
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
         if (this.GetIsBusy()
            || sender is not UserPasswordItem passwordItem
            || _passwords.Count == 1)
         {
            return;
         }

         int index = passwordItem.ViewModel.Index;
         _stackPanel.Children.Remove(passwordItem);
         _ = _passwords.Remove(passwordItem);

         for (int i = index; i < _passwords.Count; i++)
         {
            _passwords[i].ViewModel.Index = i;
         }
      }

      private void _passwordItem_UpClicked(object? sender, EventArgs e)
      {
         if (this.GetIsBusy()
            || sender is not UserPasswordItem passwordItem
            || passwordItem.ViewModel.Index == 0)
         {
            return;
         }

         _movePassword(passwordItem, passwordItem.ViewModel.Index - 1);
      }

      private void _passwordItem_DownClicked(object? sender, EventArgs e)
      {
         if (this.GetIsBusy()
            || sender is not UserPasswordItem passwordItem
            || passwordItem.ViewModel.Index == _passwords.Count - 1)
         {
            return;
         }

         _movePassword(passwordItem, passwordItem.ViewModel.Index + 1);
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()) return;

         _addPassword(string.Empty);
      }

      private void _addPassword(string password)
      {
         UserPasswordItem passwordItem = new(new() { Index = _passwords.Count, Password = password });
         passwordItem.UpClicked += _passwordItem_UpClicked;
         passwordItem.DownClicked += _passwordItem_DownClicked;
         passwordItem.DeleteClicked += _passwordItem_DeleteClicked;

         _passwords.Add(passwordItem);
         _ = _stackPanel.Children.Add(passwordItem);
         _stackPanel.UpdateLayout();

         passwordItem.Focus();
      }

      private void _movePassword(UserPasswordItem passwordItem, int index)
      {
         UserPasswordItem temp = _passwords[index];
         _passwords[index] = passwordItem;
         _passwords[passwordItem.ViewModel.Index] = temp;

         _stackPanel.Children.Clear();

         for (int i = 0; i < _passwords.Count; i++)
         {
            _passwords[i].ViewModel.Index = i;
            _ = _stackPanel.Children.Add(_passwords[i]);
         }
      }
   }
}
