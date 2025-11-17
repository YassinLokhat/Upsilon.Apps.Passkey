using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountIdentifiantsView.xaml
   /// </summary>
   public partial class AccountIdentifiantsView : UserControl
   {
      private readonly List<AccountIdentifiantItem> _identifiants = [];

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public AccountIdentifiantsView()
      {
         InitializeComponent();
      }

      public void SetDataContext(IAccount? account)
      {
         if (account is null) return;

         DataContext = account;

         _identifiants.Clear();
         _stackPanel.Children.Clear();

         if (account.Identifiants.Length == 0)
         {
            _addIdentifiant(string.Empty);
         }
         else
         {
            foreach (string identifiant in account.Identifiants)
            {
               _addIdentifiant(identifiant);
            }
         }
      }

      private void _identifiantItem_DeleteClicked(object? sender, EventArgs e)
      {
         if (sender is not AccountIdentifiantItem IdentifiantItem
            || _identifiants.Count == 1)
         {
            return;
         }

         _stackPanel.Children.Remove(IdentifiantItem);
         _ = _identifiants.Remove(IdentifiantItem);
      }

      private void _identifiantItem_UpClicked(object? sender, EventArgs e)
      {
         if (sender is null) return;

         int index = _identifiants.IndexOf((AccountIdentifiantItem)sender);

         if (index == 0) return;

         _moveIdentifiant(index, index - 1);
      }

      private void _identifiantItem_DownClicked(object? sender, EventArgs e)
      {
         if (sender is null) return;

         int index = _identifiants.IndexOf((AccountIdentifiantItem)sender);

         if (index == _identifiants.Count - 1) return;

         _moveIdentifiant(index, index + 1);
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _addIdentifiant(string.Empty);
      }

      private void _addIdentifiant(string identifiant)
      {
         AccountIdentifiantItem identifiantItem = new(new((IAccount)DataContext, identifiant));
         identifiantItem.UpClicked += _identifiantItem_UpClicked;
         identifiantItem.DownClicked += _identifiantItem_DownClicked;
         identifiantItem.DeleteClicked += _identifiantItem_DeleteClicked;
         identifiantItem.ViewModel.PropertyChanged += _viewModel_PropertyChanged;

         _identifiants.Add(identifiantItem);
         _ = _stackPanel.Children.Add(identifiantItem);
      }

      private void _viewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (DataContext is not IAccount account) return;

         account.Identifiants = [.. _identifiants.Select(x => x.ViewModel.Identifiant)];

         OnPropertyChanged(nameof(account.Identifiants));
      }

      private void _moveIdentifiant(int oldIndex, int newIndex)
      {
         (_identifiants[newIndex], _identifiants[oldIndex]) = (_identifiants[oldIndex], _identifiants[newIndex]);

         _stackPanel.Children.Clear();

         for (int i = 0; i < _identifiants.Count; i++)
         {
            _ = _stackPanel.Children.Add(_identifiants[i]);
         }
      }
   }
}
