using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for ServiceView.xaml
   /// </summary>
   public partial class ServiceView : UserControl
   {
      private ServiceViewModel? _viewModel;

      public string? GetSelectedIdentifier() => _account_AV.GetIdentifier();

      public string? GetSelectedPassword() => _account_AV.GetPassword();

      public void SetSelectedPassword(string password) => _account_AV.SetPassword(password);

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
         if (this.GetIsBusy()) return;

         MainViewModel.User.Shake();
         _account_AV.SetDataContext((AccountViewModel)_accounts_LB.SelectedItem);
      }

      private void _addAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         _accounts_LB.SelectedItem = _viewModel.AddAccount();
      }

      private void _deleteAccount_Button_Click(object sender, System.Windows.RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null
            || _accounts_LB.SelectedItem is not AccountViewModel accountViewModel
            || MessageBox.Show($"Are you sure you want to delete the account '{accountViewModel.AccountDisplay}'", "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
         {
            return;
         }

         _accounts_LB.SelectedIndex = _viewModel.DeleteAccount(accountViewModel);
      }

      private void _openUrl_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null
            || string.IsNullOrWhiteSpace(_viewModel.Url))
         {
            return;
         }

         _ = new Process()
         {
            StartInfo = new ProcessStartInfo(_viewModel.Url)
            {
               UseShellExecute = true,
            },
         }.Start();
      }

      public bool SelectAccount(string itemId)
      {
         AccountViewModel? account = _viewModel?.Accounts.FirstOrDefault(x => x.Account.ItemId == itemId);

         if (account is null) return false;

         _accounts_LB.SelectedItem = account;
         _accounts_LB.ScrollIntoView(account);

         return true;
      }

      private void _viewActivities_Button_Click(object sender, RoutedEventArgs e)
      {
         if (this.GetIsBusy()
            || _viewModel is null)
         {
            return;
         }

         if (this.GetIsBusy()) return;

         if (MainViewModel.UserActivitiesView is not null
            && MainViewModel.UserActivitiesView.IsLoaded)
         {
            UserActivitiesViewModel? vm = MainViewModel.UserActivitiesView.DataContext as UserActivitiesViewModel;
            MainViewModel.UserActivitiesView.ViewModel.RefreshFilters(_viewModel.Service.ItemId);
            MainViewModel.UserActivitiesView.Activate();
            return;
         }

         MainViewModel.UserActivitiesView = new(needsReviewFilter: false);
         MainViewModel.UserActivitiesView.ViewModel.ClearFilters();
         MainViewModel.UserActivitiesView.ViewModel.RefreshFilters(_viewModel.Service.ItemId);
         MainViewModel.UserActivitiesView.Show();
      }
   }
}
