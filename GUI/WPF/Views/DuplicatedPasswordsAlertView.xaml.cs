using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

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

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
         => _viewModel.OnLanguageChanged();

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.SelectedAlert is null
            || _alerts_LB.SelectedIndex < 0
            || _alerts_DGV.SelectedIndex < 0)
         {
            return;
         }

         AppServices.Navigation.RequestItem(
            _viewModel.SelectedAlert.Accounts[_alerts_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
