using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
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
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountIdentifiantsView.xaml
   /// </summary>
   public partial class AccountIdentifiantsView : UserControl
   {
      private ObservableCollection<AccountIdentifiantItemViewModel> _identifiants = [];

      public AccountIdentifiantsView()
      {
         InitializeComponent();

         _identifiants_LB.ItemsSource = _identifiants;
      }

      public void SetDataContext(IAccount? account)
      {
         if (account is null) return;

         DataContext = account;

         _identifiants.Clear();

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
         if (sender is not AccountIdentifiantItemViewModel IdentifiantItem
            || _identifiants.Count == 1)
         {
            return;
         }

         _ = _identifiants.Remove(IdentifiantItem);
      }

      private void _identifiantItem_UpClicked(object? sender, EventArgs e)
      {
         if (sender is null) return;

         int index = _identifiants.IndexOf((AccountIdentifiantItemViewModel)sender);
         
         if (index == 0) return;
         
         _moveIdentifiant(index, index - 1);
      }

      private void _identifiantItem_DownClicked(object? sender, EventArgs e)
      {
         if (sender is null) return;

         int index = _identifiants.IndexOf((AccountIdentifiantItemViewModel)sender);
         
         if (index == _identifiants.Count - 1) return;
         
         _moveIdentifiant(index, index + 1);
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _addIdentifiant(string.Empty);
      }

      private void _addIdentifiant(string identifiant)
      {
         IAccount account = (IAccount)DataContext;
         _identifiants.Add(new(account, identifiant));

         account.Identifiants = [.. _identifiants.Select(x => x.Identifiant)];
      }

      private void _moveIdentifiant(int oldIndex, int newIndex)
      {
         (_identifiants[newIndex], _identifiants[oldIndex]) = (_identifiants[oldIndex], _identifiants[newIndex]);

         IAccount account = (IAccount)DataContext;
         account.Identifiants = [.. _identifiants.Select(x => x.Identifiant)];
      }
   }
}
