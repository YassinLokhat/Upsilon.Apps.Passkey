using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;

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
         if (serviceViewModel is null)
         {
            DataContext = null;
            _viewModel = null;
            _accounts_LB.ItemsSource = null;

            return;
         }

         DataContext = _viewModel = serviceViewModel;
         _accounts_LB.ItemsSource = serviceViewModel.Accounts;

         if (serviceViewModel.Accounts.Count != 0)
         {
            _accounts_LB.SelectedIndex = 0;
         }
      }

      private void _accounts_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         MainViewModel.User.Shake();
         _account_AV.SetDataContext((AccountViewModel)_accounts_LB.SelectedItem);
      }

      private void _addAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_viewModel is null) return;

         _accounts_LB.SelectedItem = _viewModel.AddAccount();
      }

      private void _deleteAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_viewModel is null
            || _accounts_LB.SelectedItem is not AccountViewModel accountViewModel
            || MessageBox.Show($"Are you sure you want to delete the account '{accountViewModel.AccountDisplay}'", "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
         {
            return;
         }

         _accounts_LB.SelectedIndex = _viewModel.DeleteAccount(accountViewModel);
      }
   }
}
