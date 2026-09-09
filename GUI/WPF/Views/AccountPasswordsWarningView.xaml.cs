using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for ExpiredOrLeakedPasswordsWarningView.xaml
   /// </summary>
   internal sealed partial class AccountPasswordsWarningView : Window, ILanguageAware
   {
      private readonly AccountPasswordsWarningViewModel _viewModel;

      internal AccountPasswordsWarningView(string kind)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            Kind = kind,
         };

         _bindWarningKindCombo();

         _warnings_DGV.ItemsSource = _viewModel.Warnings;

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
         => _bindWarningKindCombo();

      private void _bindWarningKindCombo()
      {
         string selected = _viewModel.Kind;
         _warningType_CB.Items.Clear();
         _ = _warningType_CB.Items.Add(EnumHelper.ToReadableWarningKind(EnumHelper.AccountPasswordFilterAll));
         _ = _warningType_CB.Items.Add(EnumHelper.ToReadableWarningKind(WarningKinds.PasswordLeaked));
         _ = _warningType_CB.Items.Add(EnumHelper.ToReadableWarningKind(WarningKinds.PasswordUpdateReminder));
         _ = _warningType_CB.Items.Add(EnumHelper.ToReadableWarningKind(WarningKinds.WeakAccountPassword));
         _ = _warningType_CB.Items.Add(EnumHelper.ToReadableWarningKind(WarningKinds.PasskeyReusedAsAccountPassword));
         _warningType_CB.SelectedItem = EnumHelper.ToReadableWarningKind(selected);
      }

      private void _viewItemButton_Click(object sender, RoutedEventArgs e)
      {
         AppServices.Navigation.RequestItem(_viewModel.Warnings[_warnings_DGV.SelectedIndex].Account.ItemId);
      }
   }
}
