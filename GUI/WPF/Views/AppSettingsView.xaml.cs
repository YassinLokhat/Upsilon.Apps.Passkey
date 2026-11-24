using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for AppSettingsView.xaml
   /// </summary>
   [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The ViewModel is disposed in OnClosed; a modal window is never reused.")]
   internal sealed partial class AppSettingsView : Window, ILanguageAware
   {
      private readonly AppSettingsViewModel _viewModel;

      public AppSettingsView()
      {
         InitializeComponent();

         DataContext = _viewModel = new AppSettingsViewModel();
         _viewModel.CloseRequested += (_, _) => DialogResult = true;

         Loaded += (s, e) => this.PostLoadSetup();
      }

      public void OnLanguageChanged() => this.ForwardToDataContext();

      protected override void OnClosed(EventArgs e)
      {
         // Closing settings never cancels a background build; only Cancel or app exit does.
         _viewModel.Dispose();
         base.OnClosed(e);
      }

      public static void ShowAppSettings()
      {
         _ = AppServices.Dialogs.ShowDialog(new AppSettingsView());
      }

      private void _saveMenuItem_Click(object sender, RoutedEventArgs e) => _viewModel.SaveCommand.Execute(null);

      private void _resetMenuItem_Click(object sender, RoutedEventArgs e) => _viewModel.ResetCommand.Execute(null);

      private void _browseButton_Click(object sender, RoutedEventArgs e) => _viewModel.BrowseCommand.Execute(null);

      private void _value_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
         => NumericTextBoxHelper.PreviewTextInput(sender, e);

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
         => NumericTextBoxHelper.Pasting(sender, e);

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
         => NumericTextBoxHelper.TextChanged(sender, e);

      private void _offlineLeakFilterEnabled_Click(object sender, RoutedEventArgs e)
         => _viewModel.ApplyOfflineLeakFilterEnabled(_viewModel.OfflineLeakFilterEnabled);

      private void _offlineLeakFilterBuild_Click(object sender, RoutedEventArgs e) => _viewModel.BuildOfflineLeakFilterCommand.Execute(null);

      private void _offlineLeakFilterDelete_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteOfflineLeakFilterCommand.Execute(null);
   }
}