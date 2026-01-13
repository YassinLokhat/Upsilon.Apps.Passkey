using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for ExpiredOrLeakedPasswordsWarningView.xaml
   /// </summary>
   public partial class AccountPasswordsWarningView : Window
   {
      private readonly AccountPasswordsWarningViewModel _viewModel;
      private readonly Action<string> _goToItem;

      internal AccountPasswordsWarningView(WarningType warningType, Action<string> goToItem)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            WarningType = warningType,
         };

         _goToItem = goToItem;

         _ = _warningType_CB.Items.Add((WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning).ToReadableString());
         _ = _warningType_CB.Items.Add(WarningType.PasswordLeakedWarning.ToReadableString());
         _ = _warningType_CB.Items.Add(WarningType.PasswordUpdateReminderWarning.ToReadableString());

         _warnings_DGV.ItemsSource = _viewModel.Warnings;

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.WarningType = WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
         _viewModel.Text = string.Empty;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         _goToItem(_viewModel.Warnings[_warnings_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
