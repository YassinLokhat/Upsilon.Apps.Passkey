using Microsoft.Win32;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.GUI.WPF.Views
{
   /// <summary>
   /// Interaction logic for AppSettingsView.xaml
   /// </summary>
   internal sealed partial class AppSettingsView : Window
   {
      private readonly AppSettingsViewModel _viewModel;

      public AppSettingsView()
      {
         InitializeComponent();

         DataContext = _viewModel = new AppSettingsViewModel();

         Loaded += (s, e) => this.PostLoadSetup();
      }

      private void _saveMenuItem_Click(object sender, RoutedEventArgs e)
      {
         bool languageChanged = _viewModel.Save();
         if (languageChanged)
         {
            AppServices.Dialogs.Info(Strings.Msg_LanguageRestart, Strings.Title_LanguageChanged);
         }

         DialogResult = true;
      }

      private void _resetMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.Reset();
         DialogResult = true;
      }

      private void _browseButton_Click(object sender, RoutedEventArgs e)
      {
         OpenFolderDialog dialog = new()
         {
            Title = Strings.Title_BrowseDatabaseDirectory,
            InitialDirectory = _viewModel.DefaultDatabaseDirectory,
         };

         if (dialog.ShowDialog() == true)
         {
            _viewModel.DefaultDatabaseDirectory = dialog.FolderName;
         }
      }
   }
}
