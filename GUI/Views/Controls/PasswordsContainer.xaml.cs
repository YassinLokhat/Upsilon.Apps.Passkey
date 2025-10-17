using System.Windows;
using System.Windows.Controls;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for PasswordsContainer.xaml
   /// </summary>
   public partial class PasswordsContainer : UserControl
   {
      public string[] Passkeys => [.. _passwords.Select(x => x.ViewModel.Password)];

      private readonly List<PasswordItem> _passwords;

      public PasswordsContainer(string[]? passkeys)
      {
         InitializeComponent();

         _passwords = [];

         if (passkeys != null)
         {
            foreach (string password in passkeys)
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
         if (sender is not PasswordItem passwordItem
            || _passwords.Count == 1)
            return;

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
         if (sender is not PasswordItem passwordItem
            || passwordItem.ViewModel.Index == 0)
            return;

         _movePassword(passwordItem, passwordItem.ViewModel.Index - 1);
      }

      private void _passwordItem_DownClicked(object? sender, EventArgs e)
      {
         if (sender is not PasswordItem passwordItem
            || passwordItem.ViewModel.Index == _passwords.Count - 1)
            return;

         _movePassword(passwordItem, passwordItem.ViewModel.Index + 1);
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
         _ = _stackPanel.Children.Add(passwordItem);
      }

      private void _movePassword(PasswordItem passwordItem, int index)
      {
         PasswordItem temp = _passwords[index];
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
