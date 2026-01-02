using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.GUI.Themes;
using Upsilon.Apps.Passkey.GUI.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.Views
{
   /// <summary>
   /// Interaction logic for ExpiredOrLeakedPasswordsWarningView.xaml
   /// </summary>
   public partial class AccountPasswordsWarningView : Window
   {
      private readonly AccountPasswordsWarningViewModel _viewModel;
      private IAccount? _account = null;

      public AccountPasswordsWarningView(WarningType warningType)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            WarningType = warningType,
         };

         _warningType_CB.Items.Add((WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning).ToReadableString());
         _warningType_CB.Items.Add(WarningType.PasswordLeakedWarning.ToReadableString());
         _warningType_CB.Items.Add(WarningType.PasswordUpdateReminderWarning.ToReadableString());

         _warnings_DGV.ItemsSource = _viewModel.Warnings;

         Loaded += (s, e) => DarkMode.SetDarkMode(this);
      }

      public static IAccount? ShowAccountWarningsDialog(Window owner, WarningType warningType)
      {
         AccountPasswordsWarningView accountPasswordsWarningView = new(warningType)
         {
            Owner = owner,
         };

         return (accountPasswordsWarningView.ShowDialog() ?? false) ? accountPasswordsWarningView._account : null;
      }

      private void _filterClear_Button_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.WarningType = WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning;
         _viewModel.Text = string.Empty;
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         _account = _viewModel.Warnings[_warnings_DGV.SelectedIndex].Account;

         DialogResult = true;
      }
   }
}
