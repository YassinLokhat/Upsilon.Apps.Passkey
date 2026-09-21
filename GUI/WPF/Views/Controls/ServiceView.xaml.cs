using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views.Controls
{
   /// <summary>
   /// Interaction logic for ServiceView.xaml
   /// </summary>
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
   }
}
