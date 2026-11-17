using System.Windows;
using System.Windows.Controls;
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

      // True while a service switch runs: its transition already covers the
      // account and core, so the account selection inside it starts none.
      private bool _switchingService;

      internal string? GetServiceId() => _viewModel?.Service.ItemId;
      internal string? GetAccountId() => _account_AV.GetAccountId();

      internal string? GetSelectedIdentifier() => _account_AV.GetIdentifier();

      internal string? GetSelectedPassword() => _account_AV.Password;

      internal void SetSelectedPassword(string password) => _account_AV.Password = password;

      public ServiceView()
      {
         InitializeComponent();
      }

      /// <summary>
      /// The service layer's info button shows the service Id as a tooltip and
      /// copies it on click (it used to be the frame header, copied on right-click).
      /// </summary>
      private void _copyServiceId_Click(object sender, RoutedEventArgs e)
      {
         string? itemId = GetServiceId();

         if (itemId is null)
         {
            return;
         }

         AppServices.Clipboard.SetText(itemId);

         e.Handled = true;
      }

      /// <summary>
      /// Shows <paramref name="serviceViewModel"/>. <paramref name="direction"/> is
      /// where it sits in the list relative to the previous one (+1 below, -1
      /// above, 0 unknown: filtering, undo) and sets the transition's slide.
      /// </summary>
      internal void SetDataContext(ServiceViewModel? serviceViewModel, int direction = 0)
      {
         ContentTransition? transition = serviceViewModel is null || ReferenceEquals(serviceViewModel, _viewModel)
            ? null
            : ContentTransition.Capture(this);

         _switchingService = true;
         try
         {
            _setDataContext(serviceViewModel);
         }
         finally
         {
            _switchingService = false;
         }

         _ = transition?.Play(direction);
      }

      private void _setDataContext(ServiceViewModel? serviceViewModel)
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

         ContentTransition? transition = _switchingService ? null : ContentTransition.Capture(_account_AV);
         _account_AV.SetDataContext(_accounts_LB.SelectedItem as AccountViewModel);
         _ = transition?.Play(ContentTransition.Direction(_accounts_LB, e));
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
