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
   /// Interaction logic for DuplicatedPasswordWarningView.xaml
   /// </summary>
   internal sealed partial class DuplicatedPasswordsWarningView : Window, ILanguageAware
   {
      private readonly DuplicatedPasswordsWarningViewModel _viewModel;

      internal DuplicatedPasswordsWarningView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _warnings_LB.ItemsSource = _viewModel.Warnings;
         _warnings_LB.SelectionChanged += _warnings_LB_SelectionChanged;

         _warnings_LB.SelectedItem = _viewModel.Warnings.FirstOrDefault();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
      {
         object? selected = _warnings_LB.SelectedItem;
         _warnings_LB.ItemsSource = _viewModel.Warnings;
         _warnings_LB.SelectedItem = selected is DuplicatedPasswordWarningViewModel previous
            ? _viewModel.Warnings.FirstOrDefault(w => w.Accounts.Length == previous.Accounts.Length
               && ReferenceEquals(w.Accounts.FirstOrDefault()?.Account, previous.Accounts.FirstOrDefault()?.Account))
              ?? _viewModel.Warnings.FirstOrDefault()
            : _viewModel.Warnings.FirstOrDefault();
      }

      private void _warnings_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_warnings_LB.SelectedItem is not DuplicatedPasswordWarningViewModel viewModel)
         {
            return;
         }

         _warnings_DGV.ItemsSource = viewModel.Accounts;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Navigation.RequestItem(_viewModel.Warnings[_warnings_LB.SelectedIndex].Accounts[_warnings_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
