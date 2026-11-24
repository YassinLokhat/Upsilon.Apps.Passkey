using System.Windows;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Alerts;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

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

      internal void SetKind(string kind) => _viewModel.Kind = kind;

      public void OnLanguageChanged()
      {
         _viewModel.OnLanguageChanged();
         _bindAlertKindCombo();
      }

      private void _bindAlertKindCombo()
      {
         string selected = _viewModel.ReadableAlertKind;
         _alertKind_CB.ItemsSource = _viewModel.KindOptions;
         _alertKind_CB.SelectedItem = selected;
      }
   }
}
