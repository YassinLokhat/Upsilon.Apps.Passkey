using System.IO;
using System.Windows;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.Utils;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

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
         _ = _viewModel.Save();
         DialogResult = true;
      }

      private void _resetMenuItem_Click(object sender, RoutedEventArgs e)
      {
         _viewModel.Reset();
         DialogResult = true;
      }

      private void _browseButton_Click(object sender, RoutedEventArgs e)
      {
         string? defaultDatabaseDirectory = AppServices.Dialogs.PickBrowseFolder(Strings.Title_BrowseDatabaseDirectory, _viewModel.DefaultDatabaseDirectory);

         if (defaultDatabaseDirectory is not null)
         {
            _viewModel.DefaultDatabaseDirectory = defaultDatabaseDirectory;
         }
      }

      private void _offlineLeakFilterEnabled_Changed(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         AppInfo.AppSettings.LeakFilterConfig.Enabled = _viewModel.OfflineLeakFilterEnabled;

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter(AppInfo.AppSettings.LeakFilterConfig);
         }

         _viewModel.RefreshOfflineLeakFilterStatus();
      }

      private async void _offlineLeakFilterBuild_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         bool force = File.Exists(AppInfo.AppSettings.LeakFilterConfig.FilterPath);
         if (force
            && AppServices.Dialogs.Confirm(
               Strings.Msg_RebuildOfflineLeakDatabase,
               Strings.Title_RebuildOfflineLeakDatabase) != MessageBoxResult.Yes)
         {
            return;
         }

         if (!force
            && AppServices.Dialogs.Confirm(
               Strings.Msg_BuildOfflineLeakDatabase,
               Strings.Title_BuildOfflineLeakDatabase) != MessageBoxResult.Yes)
         {
            return;
         }

         _viewModel.OfflineLeakFilterBusy = true;
         _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;

         try
         {
            Progress<HibpBloomBuildProgress> progress = new(p =>
            {
               if (p.Skipped)
               {
                  _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildSkipped;
                  return;
               }

               double pct = 100.0 * p.CompletedPrefixes / p.TotalPrefixes;
               _viewModel.OfflineLeakFilterProgress = Strings.Format(
                  nameof(Strings.Msg_OfflineLeakBuildProgress),
                  pct,
                  p.CompletedPrefixes,
                  p.TotalPrefixes,
                  p.InsertedHashes);
            });

            string filterPath = AppInfo.AppSettings.LeakFilterConfig.FilterPath;
            HibpBloomBuildResult result = await HibpBloomBuilder.BuildAsync(
               filterPath,
               force: force,
               progress: progress).ConfigureAwait(true);

            AppInfo.AppSettings.LeakFilterConfig.Enabled = true;
            _viewModel.OfflineLeakFilterEnabled = true;

            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.ReloadLocalFilter(AppInfo.AppSettings.LeakFilterConfig);
            }

            _viewModel.OfflineLeakFilterProgress = result.Skipped
               ? Strings.Msg_OfflineLeakAlreadyUpToDate
               : Strings.Format(nameof(Strings.Msg_OfflineLeakBuildComplete), result.InsertedCount);
            _viewModel.RefreshOfflineLeakFilterStatus();
         }
         catch (ArgumentNullException ex)
         {
            AppServices.Dialogs.Warn(
               Strings.Format(nameof(Strings.Msg_OfflineLeakBuildFailed), ex.Message),
               Strings.Title_BuildFailed);
            _viewModel.OfflineLeakFilterProgress = Strings.Msg_BuildFailed;
         }
         finally
         {
            _viewModel.OfflineLeakFilterBusy = false;
         }
      }

      private void _offlineLeakFilterDelete_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         if (!File.Exists(AppInfo.AppSettings.LeakFilterConfig.FilterPath))
         {
            AppServices.Dialogs.Info(Strings.Msg_NoOfflineLeakDatabase, Strings.Title_OfflineLeakDatabase);
            return;
         }

         if (AppServices.Dialogs.Confirm(
               Strings.Msg_DeleteOfflineLeakDatabase,
               Strings.Title_DeleteOfflineLeakDatabase,
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.AttachLocalFilter(null);
         }

         _ = AppInfo.AppSettings.LeakFilterConfig.TryDeleteFilterFile();
         _viewModel.OfflineLeakFilterProgress = string.Empty;
         _viewModel.RefreshOfflineLeakFilterStatus();
      }
   }
}
