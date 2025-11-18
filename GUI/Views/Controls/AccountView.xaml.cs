using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for AccountView.xaml
   /// </summary>
   public partial class AccountView : UserControl
   {
      private AccountViewModel? _viewModel;

      public AccountView()
      {
         InitializeComponent();
      }

      public void SetDataContext(AccountViewModel? dataContext)
      {
         if (dataContext is null) return;

         DataContext = _viewModel = dataContext;
         _viewModel.Identifiants.Clear();

         if (_viewModel.Account.Identifiants.Length == 0)
         {
            _addIdentifiant(string.Empty);
         }
         else
         {
            foreach (string identifiant in _viewModel.Account.Identifiants)
            {
               _addIdentifiant(identifiant);
            }
         }
 
         _identifiants_LB.ItemsSource = _viewModel.Identifiants;
         _identifiants_LB.SelectedIndex = 0;
      }

      private void _identifiant_DeleteClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null
            || _viewModel.Identifiants.Count == 1)
         {
            return;
         }

         _ = _viewModel.Identifiants.Remove((IdentifiantViewModel)_identifiants_LB.SelectedItem);

         _viewModel.Account.Identifiants = [.. _viewModel.Identifiants.Select(x => x.Identifiant)];
         _identifiants_LB.SelectedIndex = 0;
      }

      private void _identifiant_UpClicked(object? sender, EventArgs e)
      {
         _moveIdentifiant(_identifiants_LB.SelectedIndex, _identifiants_LB.SelectedIndex - 1);
      }

      private void _identifiant_DownClicked(object? sender, EventArgs e)
      {
         _moveIdentifiant(_identifiants_LB.SelectedIndex, _identifiants_LB.SelectedIndex + 1);
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _addIdentifiant(string.Empty);
      }

      private void _addIdentifiant(string identifiant)
      {
         if (_viewModel is null) return;

         _viewModel.Identifiants.Add(new(_viewModel.Account, identifiant));

         _viewModel.Account.Identifiants = [.. _viewModel.Identifiants.Select(x => x.Identifiant)];
      }

      private void _moveIdentifiant(int oldIndex, int newIndex)
      {
         if (_viewModel is null
            || oldIndex < 0
            || newIndex < 0
            || newIndex >= _viewModel.Identifiants.Count)
         {
            return;
         }

         (_viewModel.Identifiants[newIndex], _viewModel.Identifiants[oldIndex]) = (_viewModel.Identifiants[oldIndex], _viewModel.Identifiants[newIndex]);

         _viewModel.Account.Identifiants = [.. _viewModel.Identifiants.Select(x => x.Identifiant)];
         _identifiants_LB.SelectedIndex = newIndex;
      }

      private void _value_TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
      {
         NumericTextBoxHelper.PreviewTextInput(sender, e);
      }

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         NumericTextBoxHelper.Pasting(sender, e);
      }

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         NumericTextBoxHelper.TextChanged(sender, e);
      }
   }
}
