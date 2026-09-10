using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for DuplicatedPasswordsAlertView.xaml
   /// </summary>
   internal sealed partial class DuplicatedPasswordsAlertView : Window, ILanguageAware
   {
      private readonly DuplicatedPasswordsAlertViewModel _viewModel;

      internal DuplicatedPasswordsAlertView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _alerts_LB.ItemsSource = _viewModel.Alerts;
         _alerts_LB.SelectionChanged += _alerts_LB_SelectionChanged;

         _alerts_LB.SelectedItem = _viewModel.Alerts.FirstOrDefault();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
      {
         object? selected = _alerts_LB.SelectedItem;
         _alerts_LB.ItemsSource = _viewModel.Alerts;
         _alerts_LB.SelectedItem = selected is DuplicatedPasswordAlertViewModel previous
            ? _viewModel.Alerts.FirstOrDefault(w => w.Accounts.Length == previous.Accounts.Length
               && ReferenceEquals(w.Accounts.FirstOrDefault()?.Account, previous.Accounts.FirstOrDefault()?.Account))
              ?? _viewModel.Alerts.FirstOrDefault()
            : _viewModel.Alerts.FirstOrDefault();
      }

      private void _alerts_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_alerts_LB.SelectedItem is not DuplicatedPasswordAlertViewModel viewModel)
         {
            return;
         }

         _alerts_DGV.ItemsSource = viewModel.Accounts;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Navigation.RequestItem(_viewModel.Alerts[_alerts_LB.SelectedIndex].Accounts[_alerts_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
