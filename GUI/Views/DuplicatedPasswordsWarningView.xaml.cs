using System.Windows;
using System.Windows.Controls;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.GUI.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for DuplicatedPasswordWarningView.xaml
   /// </summary>
   public partial class DuplicatedPasswordsWarningView : Window
   {
      private readonly DuplicatedPasswordsWarningViewModel _viewModel;
      private IAccount? _account = null;

      private DuplicatedPasswordsWarningView()
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

      public static IAccount? ShowDuplicatedPaswwordsWarningsDialog(Window owner)
      {
         DuplicatedPasswordsWarningView duplicatedPasswordWarningView = new()
         {
            Owner = owner,
         };

         return (duplicatedPasswordWarningView.ShowDialog() ?? false) ? duplicatedPasswordWarningView._account : null;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         _account = _viewModel.Warnings[_warnings_LB.SelectedIndex].Accounts[_warnings_DGV.SelectedIndex].Account;

         DialogResult = true;
      }
   }
}
