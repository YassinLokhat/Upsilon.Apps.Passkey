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

         LeakFilterConfig config = LeakFilterPaths.LoadConfig();
         config.Enabled = _viewModel.OfflineLeakFilterEnabled;
         LeakFilterPaths.SaveConfig(config);

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter();
         }

         _viewModel.RefreshOfflineLeakFilterStatus();
      }

      private async void _offlineLeakFilterBuild_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         bool force = File.Exists(LeakFilterPaths.ResolveFilterFilePath());
         if (force
            && AppServices.Dialogs.Confirm(
               "An offline leak database already exists. Rebuild it from HIBP?\nThis can take several hours and uses a large download.",
               "Rebuild offline leak database") != MessageBoxResult.Yes)
         {
            return;
         }

         if (!force
            && AppServices.Dialogs.Confirm(
               "Download the HIBP password corpus and build a local Bloom filter (~2.4 GiB)?\nThis is shared by all vault users on this machine and can take several hours.",
               "Build offline leak database") != MessageBoxResult.Yes)
         {
            return;
         }

         _viewModel.OfflineLeakFilterBusy = true;
         _viewModel.OfflineLeakFilterProgress = "Starting…";

         try
         {
            Progress<HibpBloomBuildProgress> progress = new(p =>
            {
               if (p.Skipped)
               {
                  _viewModel.OfflineLeakFilterProgress = "Skipped (file already present).";
                  return;
               }

               double pct = 100.0 * p.CompletedPrefixes / p.TotalPrefixes;
               _viewModel.OfflineLeakFilterProgress =
                  $"{pct:0.00}% · prefixes {p.CompletedPrefixes}/{p.TotalPrefixes} · hashes ≈ {p.InsertedHashes}";
            });

            string filterPath = LeakFilterPaths.ResolveFilterFilePath();
            HibpBloomBuildResult result = await HibpBloomBuilder.BuildAsync(
               filterPath,
               force: force,
               progress: progress).ConfigureAwait(true);

            LeakFilterConfig config = LeakFilterPaths.LoadConfig();
            config.Enabled = true;
            LeakFilterPaths.SaveConfig(config);
            _viewModel.OfflineLeakFilterEnabled = true;

            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.ReloadLocalFilter();
            }

            _viewModel.OfflineLeakFilterProgress = result.Skipped
               ? "Already up to date."
               : $"Build complete ({result.InsertedCount} hashes).";
            _viewModel.RefreshOfflineLeakFilterStatus();
         }
#pragma warning disable CA1031 // UI boundary: surface build failures as a dialog
         catch (Exception ex)
#pragma warning restore CA1031
         {
            AppServices.Dialogs.Warn($"Offline leak database build failed:\n{ex.Message}", "Build failed");
            _viewModel.OfflineLeakFilterProgress = "Build failed.";
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

         if (!File.Exists(LeakFilterPaths.ResolveFilterFilePath()))
         {
            AppServices.Dialogs.Info("No offline leak database file is present.", "Offline leak database");
            return;
         }

         if (AppServices.Dialogs.Confirm(
               "Permanently delete the shared offline leak database from this machine?\nThis affects all vault users. Disabling the option alone does not delete the file.",
               "Delete offline leak database",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning) != MessageBoxResult.Yes)
         {
            return;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.AttachLocalFilter(null);
         }

         _ = LeakFilterPaths.TryDeleteFilterFile();
         _viewModel.OfflineLeakFilterProgress = string.Empty;
         _viewModel.RefreshOfflineLeakFilterStatus();
      }
   }
}
