using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for DuplicatedPasswordWarningView.xaml
   /// </summary>
   public partial class DuplicatedPasswordsWarningView : Window
   {
      private readonly DuplicatedPasswordsWarningViewModel _viewModel;

      internal DuplicatedPasswordsWarningView()
      {
         InitializeComponent();

         DataContext = _viewModel = new();

         _warnings_LB.ItemsSource = _viewModel.Warnings;
         _warnings_LB.SelectionChanged += _warnings_LB_SelectionChanged;

         _warnings_LB.SelectedItem = _viewModel.Warnings.FirstOrDefault();

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      private void _warnings_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         DuplicatedPasswordWarningViewModel viewModel = (DuplicatedPasswordWarningViewModel)_warnings_LB.SelectedItem;

         _warnings_DGV.ItemsSource = viewModel.Accounts;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         MainViewModel.GoToItem?.Invoke(_viewModel.Warnings[_warnings_LB.SelectedIndex].Accounts[_warnings_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
