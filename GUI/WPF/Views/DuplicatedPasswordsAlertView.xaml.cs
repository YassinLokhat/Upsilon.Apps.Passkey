using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
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
   }
}
