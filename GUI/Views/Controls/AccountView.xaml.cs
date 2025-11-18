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
            _viewModel.AddIdentifiant(string.Empty);
         }
         else
         {
            foreach (string identifiant in _viewModel.Account.Identifiants)
            {
               _viewModel.AddIdentifiant(identifiant);
            }
         }
 
         _identifiants_LB.ItemsSource = _viewModel.Identifiants;
         _identifiants_LB.SelectedIndex = 0;
      }

      private void _identifiant_DeleteClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int index = _identifiants_LB.SelectedIndex;

         if (_viewModel.RemoveIdentifiant((IdentifiantViewModel)_identifiants_LB.SelectedItem))
         {
            _identifiants_LB.SelectedIndex = index < _viewModel.Identifiants.Count ? index : _viewModel.Identifiants.Count - 1;
         }
      }

      private void _identifiant_UpClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiants_LB.SelectedIndex - 1;

         if (_viewModel.MoveIdentifiant(_identifiants_LB.SelectedIndex, newIndex))
         {
            _identifiants_LB.SelectedIndex = newIndex;
         }
      }

      private void _identifiant_DownClicked(object? sender, EventArgs e)
      {
         if (_viewModel is null) return;

         int newIndex = _identifiants_LB.SelectedIndex + 1;

         if (_viewModel.MoveIdentifiant(_identifiants_LB.SelectedIndex, newIndex))
         {
            _identifiants_LB.SelectedIndex = newIndex;
         }
      }

      private void _addButton_Click(object sender, RoutedEventArgs e)
      {
         _viewModel?.AddIdentifiant(string.Empty);
         _identifiants_LB.SelectedIndex = _identifiants_LB.Items.Count - 1;
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
