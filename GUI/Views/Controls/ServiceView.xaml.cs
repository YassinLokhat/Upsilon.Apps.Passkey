using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.GUI.Views.Controls
{
   /// <summary>
   /// Interaction logic for ServiceView.xaml
   /// </summary>
   public partial class ServiceView : UserControl
   {
      private ServiceViewModel? _viewModel;

      public ServiceView()
      {
         InitializeComponent();
      }

      internal void SetDataContext(ServiceViewModel? serviceViewModel)
      {
         if (serviceViewModel is null) return;

         DataContext = _viewModel = serviceViewModel;
         _accounts_LB.ItemsSource = serviceViewModel.Accounts;

         if (serviceViewModel.Accounts.Count != 0)
         {
            _accounts_LB.SelectedIndex = 0;
         }
      }

      private void _accounts_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         _ = MainViewModel.User.ItemId;
         _account_AV.SetDataContext((AccountViewModel)_accounts_LB.SelectedItem);
      }

      private void _addAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_viewModel is null) return;

         AccountViewModel? accountModel = _viewModel.Accounts.FirstOrDefault(x => x.Identifiants.Any(y => y.Identifiant == "NewAccount"));

         if (accountModel == null)
         {
            accountModel = new(_viewModel.Service.AddAccount(["NewAccount"]));
            _viewModel.Accounts.Insert(0, accountModel);
         }

         _accounts_LB.SelectedItem = accountModel;
      }

      private void _deleteAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_viewModel is null
            || _viewModel.Accounts.Count == 1
            || _accounts_LB.SelectedItem is not AccountViewModel accountModel
            || MessageBox.Show($"Are you sure you want to delete the account '{accountModel.AccountDisplay}'", "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
         {
            return;
         }

         int index = _accounts_LB.SelectedIndex;

         _viewModel.Accounts.Remove(accountModel);
         _viewModel.Service.DeleteAccount(accountModel.Account);

         _accounts_LB.SelectedIndex = index < _viewModel.Accounts.Count ? index : _viewModel.Accounts.Count - 1;
      }
   }
}
