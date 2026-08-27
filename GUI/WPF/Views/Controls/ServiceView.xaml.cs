using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for ServiceView.xaml
   /// </summary>
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by WPF via XAML/BAML.")]
   internal sealed partial class ServiceView : UserControl
   {
      private ServiceViewModel? _viewModel;

      internal string? GetServiceId() => _viewModel?.Service.ItemId;
      internal string? GetAccountId() => _account_AV.GetAccountId();

      internal string? GetSelectedIdentifier() => _account_AV.GetIdentifier();

      internal string? GetSelectedPassword() => _account_AV.Password;

      internal void SetSelectedPassword(string password) => _account_AV.Password = password;

      public ServiceView()
      {
         InitializeComponent();
      }

      private void _serviceView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
      {
         string sourceText = (e.OriginalSource as TextBlock)?.Text ?? string.Empty;

         if (sourceText != _service_GB.Header.ToString())
         {
            return;
         }

         string? itemId = GetServiceId();

         if (itemId is null)
         {
            return;
         }

         AppServices.Clipboard.SetText(itemId);

         e.Handled = true;
      }

      internal void SetDataContext(ServiceViewModel? serviceViewModel)
      {
         if (serviceViewModel is null)
         {
            DataContext = null;
            _viewModel = null;
            _accounts_LB.ItemsSource = null;
            _account_AV.SetDataContext(null);

            return;
         }

         DataContext = _viewModel = serviceViewModel;
         _accounts_LB.ItemsSource = serviceViewModel.Accounts;

         if (serviceViewModel.Accounts.Count != 0)
         {
            _accounts_LB.SelectedIndex = 0;
         }
         else
         {
            _account_AV.SetDataContext(null);
         }
      }

      private void _accounts_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (this.GetIsBusy())
         {
            return;
         }

         AppServices.Session.User?.Shake();
         _account_AV.SetDataContext(_accounts_LB.SelectedItem as AccountViewModel);
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
            || AppServices.Dialogs.Confirm(Strings.Format(nameof(Strings.Msg_DeleteAccount), accountViewModel.AccountDisplay), Strings.Title_DeleteAccount) != MessageBoxResult.Yes)
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

         using Process process = new()
         {
            StartInfo = new ProcessStartInfo(_viewModel.Url)
            {
               UseShellExecute = true,
            },
         };

         _ = process.Start();
      }

      public bool SelectAccount(string itemId)
      {
         AccountViewModel? account = _viewModel?.Accounts.FirstOrDefault(x => x.Account.ItemId == itemId);

         if (account is null)
         {
            return false;
         }

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

         string itemId = _viewModel.Service.ItemId;

         _ = AppServices.Dialogs.ShowSingleton(
            factory: () =>
            {
               UserActivitiesView view = new(needsReviewFilter: false);
               view.ViewModel.ClearFilters();
               view.ViewModel.SearchCriteria = itemId;
               return view;
            },
            configure: view => view.ViewModel.SearchCriteria = itemId);
      }
   }
}
