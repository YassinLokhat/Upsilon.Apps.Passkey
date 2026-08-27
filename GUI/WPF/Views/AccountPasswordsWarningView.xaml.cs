using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for ExpiredOrLeakedPasswordsWarningView.xaml
   /// </summary>
   internal sealed partial class AccountPasswordsWarningView : Window, ILanguageAware
   {
      private readonly AccountPasswordsWarningViewModel _viewModel;

      internal AccountPasswordsWarningView(WarningType warningType)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            WarningType = warningType,
         };

         _bindWarningTypeCombo();

         _warnings_DGV.ItemsSource = _viewModel.Warnings;

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
         => _bindWarningTypeCombo();

      private void _bindWarningTypeCombo()
      {
         WarningType selected = _viewModel.WarningType;
         _warningType_CB.Items.Clear();
         _ = _warningType_CB.Items.Add((WarningType.PasswordUpdateReminderWarning | WarningType.PasswordLeakedWarning).ToReadableString());
         _ = _warningType_CB.Items.Add(WarningType.PasswordLeakedWarning.ToReadableString());
         _ = _warningType_CB.Items.Add(WarningType.PasswordUpdateReminderWarning.ToReadableString());
         _warningType_CB.SelectedItem = selected.ToReadableString();
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Navigation.RequestItem(_viewModel.Warnings[_warnings_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
