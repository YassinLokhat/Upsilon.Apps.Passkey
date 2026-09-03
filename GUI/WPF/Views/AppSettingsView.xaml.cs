using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

         Loaded += (s, e) =>
         {
            this.PostLoadSetup();
            OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
            update.BusyChanged += _offlineLeakFilterUpdate_BusyChanged;
            update.ProgressChanged += _offlineLeakFilterUpdate_ProgressChanged;
            _syncBusyFromService();
            _applyProgressFromService();
         };

         Unloaded += (s, e) => _unsubscribeUpdateService();
      }

      protected override void OnClosed(EventArgs e)
      {
         _unsubscribeUpdateService();
         // Do not cancel a background auto-update when closing settings; only the
         // Cancel button (or app exit) stops an in-flight run.
         base.OnClosed(e);
      }

      public static void ShowAppSettings(Window owner)
      {
         _ = new AppSettingsView
         {
            Owner = owner,
         }
         .ShowDialog();
      }

      private void _unsubscribeUpdateService()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         update.BusyChanged -= _offlineLeakFilterUpdate_BusyChanged;
         update.ProgressChanged -= _offlineLeakFilterUpdate_ProgressChanged;
      }

      private void _offlineLeakFilterUpdate_BusyChanged(object? sender, EventArgs e)
         => Dispatcher.Invoke(() =>
         {
            _syncBusyFromService();
            _applyProgressFromService();
         });

      private void _offlineLeakFilterUpdate_ProgressChanged(object? sender, EventArgs e)
         => Dispatcher.Invoke(_applyProgressFromService);

      private void _syncBusyFromService()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         bool busy = update.IsBusy;
         _viewModel.OfflineLeakFilterBusy = busy;

         if (!busy)
         {
            _viewModel.RefreshOfflineLeakFilterStatus();

            if (update.WasCancelled)
            {
               _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
            }
            else if (update.LatestResult is { } result)
            {
               _viewModel.OfflineLeakFilterProgress = _completionMessage(result);
            }
         }
      }

      private void _applyProgressFromService()
      {
         if (!AppServices.OfflineLeakFilterUpdate.IsBusy)
         {
            return;
         }

         if (AppServices.OfflineLeakFilterUpdate.LatestProgress is { } progress)
         {
            _viewModel.OfflineLeakFilterProgress = _formatProgress(progress);
         }
         else if (string.IsNullOrEmpty(_viewModel.OfflineLeakFilterProgress))
         {
            _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;
         }
      }

      private void _saveMenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         _ = _viewModel.Save();
         DialogResult = true;
      }

      private void _resetMenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         _viewModel.Reset();
         DialogResult = true;
      }

      private void _browseButton_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         string? defaultDatabaseDirectory = AppServices.Dialogs.PickBrowseFolder(Strings.Title_BrowseDatabaseDirectory, _viewModel.DefaultDatabaseDirectory);

         if (defaultDatabaseDirectory is not null)
         {
            _viewModel.DefaultDatabaseDirectory = defaultDatabaseDirectory;
         }
      }

      private void _value_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
         => NumericTextBoxHelper.PreviewTextInput(sender, e);

      private void _value_TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
         => NumericTextBoxHelper.Pasting(sender, e);

      private void _value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
         => NumericTextBoxHelper.TextChanged(sender, e);

      private void _offlineLeakFilterEnabled_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         // Click is user-only (binding updates do not raise it). Turning the
         // offline filter on also opts the user into background refreshes;
         // turning it off clears auto-update so a later enable is an explicit choice again.
         AppInfo.AppSettings.LeakFilterConfig.Enabled = _viewModel.OfflineLeakFilterEnabled;

         if (_viewModel.OfflineLeakFilterEnabled)
         {
            _viewModel.OfflineLeakFilterAutoUpdateEnabled = true;
            AppInfo.AppSettings.LeakFilterConfig.AutoUpdateEnabled = true;
         }
         else
         {
            _viewModel.OfflineLeakFilterAutoUpdateEnabled = false;
            AppInfo.AppSettings.LeakFilterConfig.AutoUpdateEnabled = false;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter(AppInfo.AppSettings.LeakFilterConfig);
         }

         _viewModel.RefreshOfflineLeakFilterStatus();
         AppServices.Session.Database?.RefreshWarnings();
      }

      private void _offlineLeakFilterAutoUpdate_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            return;
         }

         AppInfo.AppSettings.LeakFilterConfig.AutoUpdateEnabled = _viewModel.OfflineLeakFilterAutoUpdateEnabled;
      }

      private async void _offlineLeakFilterBuild_Click(object sender, RoutedEventArgs e)
      {
         if (_viewModel.OfflineLeakFilterBusy)
         {
            AppServices.OfflineLeakFilterUpdate.Cancel();
            return;
         }

         // An existing database is refreshed range by range against the ETags of
         // the last run, never rebuilt: only what changed comes back down.
         bool update = File.Exists(AppInfo.AppSettings.LeakFilterConfig.FilterPath);

         if (AppServices.Dialogs.Confirm(
               update ? Strings.Msg_UpdateOfflineLeakDatabase : Strings.Msg_BuildOfflineLeakDatabase,
               update ? Strings.Title_UpdateOfflineLeakDatabase : Strings.Title_BuildOfflineLeakDatabase)
            != MessageBoxResult.Yes)
         {
            return;
         }

         _viewModel.OfflineLeakFilterBusy = true;
         _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;

         try
         {
            HibpBloomBuildResult? result = await AppServices.OfflineLeakFilterUpdate.RunAsync(
               update ? HibpBloomBuildMode.Update : HibpBloomBuildMode.BuildIfMissing,
               progress: null,
               cancellationToken: CancellationToken.None).ConfigureAwait(true);

            if (result is null)
            {
               // Another run claimed the slot (e.g. auto-update). Reflect that.
               _syncBusyFromService();
               _applyProgressFromService();
               return;
            }

            AppInfo.AppSettings.LeakFilterConfig.Enabled = true;
            _viewModel.OfflineLeakFilterEnabled = true;

            // After a successful first full build, turn auto-update on by default
            // so later startups refresh the corpus without another multi-GiB download.
            if (!result.Value.Skipped && !result.Value.IsRefresh)
            {
               AppInfo.AppSettings.LeakFilterConfig.AutoUpdateEnabled = true;
               _viewModel.OfflineLeakFilterAutoUpdateEnabled = true;
               AppInfo.AppSettings.Save(AppInfo.ConfigFile);
            }

            _viewModel.OfflineLeakFilterProgress = _completionMessage(result.Value);
         }
         catch (OperationCanceledException)
         {
            _viewModel.OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
         }
         // Hours of downloading and writing: a transport, disk or path failure must
         // surface as a warning instead of tearing down the app.
         catch (Exception ex)
            when (ex is ArgumentException
            or HttpRequestException
            or IOException
            or UnauthorizedAccessException)
         {
            AppServices.Dialogs.Warn(
               Strings.Format(nameof(Strings.Msg_OfflineLeakBuildFailed), ex.Message),
               Strings.Title_BuildFailed);
            _viewModel.OfflineLeakFilterProgress = Strings.Msg_BuildFailed;
         }
         finally
         {
            _syncBusyFromService();
            AppServices.Session.Database?.RefreshWarnings();
         }
      }

      private static double _mebibytes(long bytes) => bytes / (1024.0 * 1024.0);

      private static string _formatProgress(HibpBloomBuildProgress progress)
      {
         if (progress.Skipped)
         {
            return Strings.Msg_OfflineLeakBuildSkipped;
         }

         double pct = 100.0 * progress.CompletedPrefixes / progress.TotalPrefixes;
         return progress.IsRefresh
            ? Strings.Format(
               nameof(Strings.Msg_OfflineLeakUpdateProgress),
               pct,
               progress.CompletedPrefixes,
               progress.TotalPrefixes,
               progress.UnchangedPrefixes,
               progress.ChangedPrefixes,
               _mebibytes(progress.DownloadedBytes))
            : Strings.Format(
               nameof(Strings.Msg_OfflineLeakBuildProgress),
               pct,
               progress.CompletedPrefixes,
               progress.TotalPrefixes,
               progress.InsertedHashes);
      }

      private static string _completionMessage(HibpBloomBuildResult result)
      {
         return result.Skipped
            ? Strings.Msg_OfflineLeakAlreadyUpToDate
            : result.IsRefresh
            ? Strings.Format(
               nameof(Strings.Msg_OfflineLeakUpdateComplete),
               result.ChangedPrefixes,
               result.UnchangedPrefixes,
               _mebibytes(result.DownloadedBytes))
            : Strings.Format(nameof(Strings.Msg_OfflineLeakBuildComplete), result.InsertedCount);
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
         AppServices.Session.Database?.RefreshWarnings();
      }
   }
}
