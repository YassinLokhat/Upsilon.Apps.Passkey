using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for AccountPasswordsAlertView.xaml
   /// </summary>
   internal sealed partial class AccountPasswordsAlertView : Window, ILanguageAware
   {
      private readonly AccountPasswordsAlertViewModel _viewModel;

      internal AccountPasswordsAlertView(string kind)
      {
         InitializeComponent();

         DataContext = _viewModel = new()
         {
            Kind = kind,
         };

         _bindAlertKindCombo();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged()
         => _bindAlertKindCombo();

      private void _bindAlertKindCombo()
      {
         string selected = _viewModel.Kind;
         _alertKind_CB.Items.Clear();
         _ = _alertKind_CB.Items.Add(EnumHelper.ToReadableAlertKind(EnumHelper.AccountPasswordFilterAll));
         _ = _alertKind_CB.Items.Add(EnumHelper.ToReadableAlertKind(AlertKinds.PasswordLeaked));
         _ = _alertKind_CB.Items.Add(EnumHelper.ToReadableAlertKind(AlertKinds.PasswordUpdateReminder));
         _ = _alertKind_CB.Items.Add(EnumHelper.ToReadableAlertKind(AlertKinds.WeakAccountPassword));
         _ = _alertKind_CB.Items.Add(EnumHelper.ToReadableAlertKind(AlertKinds.PasskeyReusedAsAccountPassword));
         _alertKind_CB.SelectedItem = EnumHelper.ToReadableAlertKind(selected);
      }
   }
}
