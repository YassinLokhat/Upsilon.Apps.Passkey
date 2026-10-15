using System.Globalization;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using System.Net.Http;
using Upsilon.Apps.Passkey.Utils;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>
   /// Application settings. Edits stay in this ViewModel until <see cref="Save"/>,
   /// so closing without saving changes nothing (including the login idle timeout).
   /// The offline leak filter section is only meaningful when
   /// <see cref="IHostCapabilities.SupportsOfflineLeakFilter"/> is set.
   /// </summary>
   internal sealed class AppSettingsViewModel : ObservableObject, ILanguageRefreshable, IDisposable
   {
      public AppSettingsViewModel()
      {
         _loadFrom(_store.Current);
         RefreshOfflineLeakFilterStatus();

         SaveCommand = new RelayCommand(_ => _close(Save), _ => OfflineLeakFilterIdle);
         ResetCommand = new RelayCommand(_ => _close(Reset), _ => OfflineLeakFilterIdle);
         BrowseCommand = new AsyncRelayCommand(BrowseDefaultDirectoryAsync, () => OfflineLeakFilterIdle && SupportsFileSystemPaths);
         BuildOfflineLeakFilterCommand = new AsyncRelayCommand(BuildOrCancelOfflineLeakFilterAsync, () => SupportsOfflineLeakFilter);
         DeleteOfflineLeakFilterCommand = new AsyncRelayCommand(DeleteOfflineLeakFilterAsync, () => OfflineLeakFilterIdle && SupportsOfflineLeakFilter);

         if (SupportsOfflineLeakFilter)
         {
            OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
            update.BusyChanged += _onUpdateBusyChanged;
            update.ProgressChanged += _onUpdateProgressChanged;
            _syncBusyFromService();
            _applyProgressFromService();
         }
      }

      public RelayCommand SaveCommand { get; }
      public RelayCommand ResetCommand { get; }
      public AsyncRelayCommand BrowseCommand { get; }
      public AsyncRelayCommand BuildOfflineLeakFilterCommand { get; }
      public AsyncRelayCommand DeleteOfflineLeakFilterCommand { get; }

      /// <summary>Raised after Save or Reset so the host closes the settings view.</summary>
      public event EventHandler? CloseRequested;

      private static IAppSettingsStore _store
         => AppServices.Settings ?? throw new InvalidOperationException("No application settings store registered.");

      public bool SupportsOfflineLeakFilter { get; } = AppServices.Capabilities.SupportsOfflineLeakFilter;

      public bool SupportsFileSystemPaths { get; } = AppServices.Capabilities.SupportsFileSystemPaths;

      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = Strings.Format(nameof(Strings.Title_AppSettings), AppServices.Info.Title);

      public IReadOnlyList<AppLanguage> Languages
      {
         get;
         private set => SetProperty(ref field, value);
      } = LocalizationService.Supported;

      public IReadOnlyList<AppThemeOption> Themes
      {
         get;
         private set => SetProperty(ref field, value);
      } = ThemePreferences.Options;

      public string DefaultDatabaseDirectory
      {
         get;
         set
         {
            if (!Directory.Exists(value))
            {
               value = _store.CreateDefault().DefaultDatabaseDirectory;
            }

            _ = SetProperty(ref field, value);
         }
      } = string.Empty;

      public AppLanguage SelectedLanguage
      {
         get;
         set
         {
            if (value is not null)
            {
               _ = SetProperty(ref field, value);
            }
         }
      } = LocalizationService.GetLanguageOrDefault(null);

      public AppThemeOption SelectedTheme
      {
         get;
         set
         {
            if (value is not null)
            {
               _ = SetProperty(ref field, value);
            }
         }
      } = ThemePreferences.Options[0];

      /// <summary>Seconds of login-screen inactivity before credentials reset; <c>0</c> disables it.</summary>
      public int LoginIdleTimeoutSeconds
      {
         get;
         set => SetProperty(ref field, Math.Max(0, value));
      }

      public bool OfflineLeakFilterEnabled
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(OfflineLeakFilterAutoUpdateEditable));
            }
         }
      }

      public int OfflineLeakFilterAutoUpdateFrequency
      {
         get;
         set => SetProperty(ref field, Math.Max(0, value));
      }

      public IReadOnlyList<LeakFilterQualityOption> LeakFilterQualityOptions
      {
         get;
         private set => SetProperty(ref field, value);
      } = _qualityOptions();

      public LeakFilterQualityOption SelectedLeakFilterQuality
      {
         get;
         set
         {
            if (value is not null)
            {
               _ = SetProperty(ref field, value);
            }
         }
      } = _qualityOptionOrDefault(null);

      public bool OfflineLeakFilterAutoUpdateEditable
         => OfflineLeakFilterEnabled && OfflineLeakFilterIdle;

      public string OfflineLeakFilterStatus
      {
         get;
         set => SetProperty(ref field, value);
      } = Strings.Msg_OfflineLeakStatusUnknown;

      public bool OfflineLeakFilterBusy
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(OfflineLeakFilterIdle));
               OnPropertyChanged(nameof(OfflineLeakFilterBuildButtonText));
               OnPropertyChanged(nameof(OfflineLeakFilterAutoUpdateEditable));
               SaveCommand?.RaiseCanExecuteChanged();
               ResetCommand?.RaiseCanExecuteChanged();
               BrowseCommand?.RaiseCanExecuteChanged();
               DeleteOfflineLeakFilterCommand?.RaiseCanExecuteChanged();
            }
         }
      }

      public bool OfflineLeakFilterIdle => !OfflineLeakFilterBusy;

      public string OfflineLeakFilterBuildButtonText
         => OfflineLeakFilterBusy ? Strings.Button_Cancel : Strings.Label_BuildUpdate;

      public string OfflineLeakFilterProgress
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public void OnLanguageChanged()
      {
         string languageCode = SelectedLanguage.Code;
         string themeCode = SelectedTheme.Code;
         string qualityCode = SelectedLeakFilterQuality.Code;
         Title = Strings.Format(nameof(Strings.Title_AppSettings), AppServices.Info.Title);
         Languages = LocalizationService.Supported;
         Themes = ThemePreferences.Options;
         LeakFilterQualityOptions = _qualityOptions();
         SelectedLeakFilterQuality = _qualityOptionOrDefault(qualityCode);
         SelectedLanguage = LocalizationService.GetLanguageOrDefault(languageCode);
         SelectedTheme = ThemePreferences.GetOptionOrDefault(themeCode);
         OnPropertyChanged(nameof(OfflineLeakFilterBuildButtonText));
         RefreshOfflineLeakFilterStatus();
      }

      /// <summary>
      /// Writes the edited values to the store, persists them and applies the
      /// effective language and theme. Returns <see langword="true"/> when the culture changed.
      /// </summary>
      public bool Save()
      {
         AppSettings settings = _store.Current;
         settings.DefaultDatabaseDirectory = DefaultDatabaseDirectory;
         settings.Language = SelectedLanguage.Code;
         settings.Theme = SelectedTheme.Code;
         settings.LoginIdleTimeoutSeconds = LoginIdleTimeoutSeconds;
         settings.LocalLeakDatabaseAutoUpdateFrequency = OfflineLeakFilterAutoUpdateFrequency;
         settings.LocalLeakDatabaseQuality = SelectedLeakFilterQuality.Code;
         return _persistAndApply();
      }

      public void Reset()
      {
         _store.Current = _store.CreateDefault();
         _loadFrom(_store.Current);
         RefreshOfflineLeakFilterStatus();
         _ = _persistAndApply();
      }

      public void RefreshOfflineLeakFilterStatus()
      {
         LeakFilterConfig config = _store.Current.LeakFilterConfig;
         OfflineLeakFilterEnabled = config.Enabled;

         string path = config.FilterPath;
         if (!AppServices.Capabilities.SupportsOfflineLeakFilter || !File.Exists(path))
         {
            OfflineLeakFilterStatus = Strings.Msg_OfflineLeakFileAbsent;
            return;
         }

         FileInfo info = new(path);
         double sizeGiB = info.Length / (1024d * 1024d * 1024d);

         // The .pkbf header stamp survives copies, AV scans and restores that move LastWriteTimeUtc.
         DateTime updatedUtc = config.TryGetBuiltUtc(out DateTime builtUtc) ? builtUtc : info.LastWriteTimeUtc;
         string updated = updatedUtc.ToLocalTime().ToString(Strings.Activity_DateTimeFormat, CultureInfo.InvariantCulture);

         OfflineLeakFilterStatus = config.Enabled
            ? Strings.Format(nameof(Strings.Msg_OfflineLeakFilePresent), sizeGiB, updated)
            : Strings.Format(nameof(Strings.Msg_OfflineLeakFilePresentDisabled), sizeGiB, updated);
      }

      public async Task BrowseDefaultDirectoryAsync()
      {
         if (OfflineLeakFilterBusy || AppServices.Dialogs is not { } dialogs)
         {
            return;
         }

         string? directory = await dialogs.PickBrowseFolderAsync(Strings.Title_BrowseDatabaseDirectory, DefaultDatabaseDirectory).ConfigureAwait(true);
         if (directory is not null)
         {
            DefaultDatabaseDirectory = directory;
         }
      }

      /// <summary>
      /// Applies the offline filter switch immediately: turning it on also allows
      /// background refreshes, turning it off makes a later enable an explicit choice again.
      /// Does nothing when the stored value already matches, so binding echoes are harmless.
      /// </summary>
      public void ApplyOfflineLeakFilterEnabled(bool enabled)
      {
         if (OfflineLeakFilterBusy || !SupportsOfflineLeakFilter)
         {
            return;
         }

         LeakFilterConfig config = _store.Current.LeakFilterConfig;
         if (config.Enabled == enabled)
         {
            return;
         }

         config.Enabled = enabled;
         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter(config);
         }

         RefreshOfflineLeakFilterStatus();
         _refreshAlerts();
      }

      public async Task BuildOrCancelOfflineLeakFilterAsync()
      {
         if (!SupportsOfflineLeakFilter || AppServices.Dialogs is not { } dialogs)
         {
            return;
         }

         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         if (OfflineLeakFilterBusy)
         {
            update.Cancel();
            OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
            return;
         }

         // The build sizes the filter from the quality the user sees, even before Save.
         AppSettings settings = _store.Current;
         settings.LocalLeakDatabaseQuality = SelectedLeakFilterQuality.Code;
         LeakFilterConfig config = settings.LeakFilterConfig;
         double approxGiB = LeakFilterQuality.ApproximateSizeGiB(config.FalsePositiveRate);

         // Without the .ranges sidecar, or after a quality change, an update cannot be incremental.
         bool hasFilter = File.Exists(config.FilterPath);
         bool hasSidecar = hasFilter && File.Exists(HibpBloomBuilder.GetRangeStatePath(config.FilterPath));
         bool sizingMatches = hasFilter && LeakFilterQuality.FileSizingMatches(config.FilterPath, BloomSizing.DefaultCapacity, config.FalsePositiveRate);

         (HibpBloomBuildMode mode, string message, string title) = (hasFilter, hasSidecar && sizingMatches, sizingMatches) switch
         {
            (false, _, _) => (HibpBloomBuildMode.BuildIfMissing, Strings.Format(nameof(Strings.Msg_BuildOfflineLeakDatabase), approxGiB), Strings.Title_BuildOfflineLeakDatabase),
            (true, true, _) => (HibpBloomBuildMode.Update, Strings.Msg_UpdateOfflineLeakDatabase, Strings.Title_UpdateOfflineLeakDatabase),
            (true, false, false) => (HibpBloomBuildMode.Rebuild, Strings.Format(nameof(Strings.Msg_RebuildOfflineLeakDatabaseQuality), approxGiB), Strings.Title_RebuildOfflineLeakDatabaseQuality),
            _ => (HibpBloomBuildMode.Rebuild, Strings.Format(nameof(Strings.Msg_BuildOfflineLeakDatabase), approxGiB), Strings.Title_BuildOfflineLeakDatabase),
         };

         if (await dialogs.ConfirmAsync(message, title).ConfigureAwait(true) != DialogResult.Yes)
         {
            return;
         }

         OfflineLeakFilterBusy = true;
         OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;
         try
         {
            HibpBloomBuildResult? result = await update.RunAsync(mode, progress: null, CancellationToken.None).ConfigureAwait(true);
            if (result is null)
            {
               _syncBusyFromService();
               _applyProgressFromService();
               return;
            }

            config.Enabled = true;
            OfflineLeakFilterEnabled = true;

            // After a full build, refresh automatically so later startups avoid another multi-GiB download.
            if (!result.Value.Skipped && !result.Value.IsRefresh)
            {
               config.AutoUpdateFrequency = OfflineLeakFilterAutoUpdateFrequency = 7;
               _store.Save();
            }

            OfflineLeakFilterProgress = _completionMessage(result.Value);
         }
         catch (OperationCanceledException)
         {
            OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
         }
         // Hours of downloading and writing: transport, disk or corruption failures become a warning.
         catch (Exception ex)
            when (ex is ArgumentException
            or HibpBloomCorruptException
            or HttpRequestException
            or IOException
            or InvalidDataException
            or UnauthorizedAccessException)
         {
            await dialogs.WarnAsync(Strings.Format(nameof(Strings.Msg_OfflineLeakBuildFailed), ErrorText.Describe(ex)), Strings.Title_BuildFailed).ConfigureAwait(true);
            OfflineLeakFilterProgress = Strings.Msg_BuildFailed;
         }
         finally
         {
            _syncBusyFromService();
            _refreshAlerts();
         }
      }

      public async Task DeleteOfflineLeakFilterAsync()
      {
         if (OfflineLeakFilterBusy || !SupportsOfflineLeakFilter || AppServices.Dialogs is not { } dialogs)
         {
            return;
         }

         LeakFilterConfig config = _store.Current.LeakFilterConfig;
         if (!File.Exists(config.FilterPath))
         {
            await dialogs.InfoAsync(Strings.Msg_NoOfflineLeakDatabase, Strings.Title_OfflineLeakDatabase).ConfigureAwait(true);
            return;
         }

         if (await dialogs.ConfirmAsync(Strings.Msg_DeleteOfflineLeakDatabase, Strings.Title_DeleteOfflineLeakDatabase, DialogButtons.YesNo, DialogIcon.Warning).ConfigureAwait(true) != DialogResult.Yes)
         {
            return;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.AttachLocalFilter(null);
         }

         _ = config.TryDeleteFilterFile();
         OfflineLeakFilterProgress = string.Empty;
         RefreshOfflineLeakFilterStatus();
         _refreshAlerts();
      }

      public void Dispose()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         update.BusyChanged -= _onUpdateBusyChanged;
         update.ProgressChanged -= _onUpdateProgressChanged;
         CloseRequested = null;
      }

      private void _close(Action action)
      {
         if (OfflineLeakFilterBusy)
         {
            return;
         }

         action();
         CloseRequested?.Invoke(this, EventArgs.Empty);
      }

      private void _close(Func<bool> action) => _close(() => { _ = action(); });

      private static void _onUi(Action action)
      {
         if (AppServices.Ui is { } ui)
         {
            ui.Post(action);
         }
         else
         {
            action();
         }
      }

      private void _onUpdateBusyChanged(object? sender, EventArgs e) => _onUi(() =>
      {
         _syncBusyFromService();
         _applyProgressFromService();
      });

      private void _onUpdateProgressChanged(object? sender, EventArgs e) => _onUi(_applyProgressFromService);

      private void _syncBusyFromService()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         OfflineLeakFilterBusy = update.IsBusy;
         if (update.IsBusy)
         {
            return;
         }

         RefreshOfflineLeakFilterStatus();
         if (update.WasCancelled)
         {
            OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
         }
         else if (update.LatestResult is { } result)
         {
            OfflineLeakFilterProgress = _completionMessage(result);
         }
      }

      private void _applyProgressFromService()
      {
         OfflineLeakFilterUpdateService update = AppServices.OfflineLeakFilterUpdate;
         if (!update.IsBusy)
         {
            return;
         }

         if (update.IsCancellationRequested)
         {
            OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildCancelled;
         }
         else if (update.LatestProgress is { } progress)
         {
            OfflineLeakFilterProgress = _formatProgress(progress);
         }
         else if (string.IsNullOrEmpty(OfflineLeakFilterProgress))
         {
            OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;
         }
      }

      private static void _refreshAlerts()
      {
         AppServices.Session.Database?.RefreshAlerts();
         AppServices.Session.Alerts.RefreshHostAlerts();
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
            ? Strings.Format(nameof(Strings.Msg_OfflineLeakUpdateProgress), pct, progress.CompletedPrefixes, progress.TotalPrefixes, progress.UnchangedPrefixes, progress.ChangedPrefixes, _mebibytes(progress.DownloadedBytes))
            : Strings.Format(nameof(Strings.Msg_OfflineLeakBuildProgress), pct, progress.CompletedPrefixes, progress.TotalPrefixes, progress.InsertedHashes);
      }

      private static string _completionMessage(HibpBloomBuildResult result)
         => result.Skipped
            ? Strings.Msg_OfflineLeakAlreadyUpToDate
            : result.IsRefresh
            ? Strings.Format(nameof(Strings.Msg_OfflineLeakUpdateComplete), result.ChangedPrefixes, result.UnchangedPrefixes, _mebibytes(result.DownloadedBytes))
            : Strings.Format(nameof(Strings.Msg_OfflineLeakBuildComplete), result.InsertedCount);

      private void _loadFrom(AppSettings settings)
      {
         DefaultDatabaseDirectory = settings.DefaultDatabaseDirectory;
         SelectedLanguage = LocalizationService.GetLanguageOrDefault(settings.Language);
         SelectedTheme = ThemePreferences.GetOptionOrDefault(settings.Theme);
         LoginIdleTimeoutSeconds = settings.LoginIdleTimeoutSeconds;
         OfflineLeakFilterAutoUpdateFrequency = settings.LocalLeakDatabaseAutoUpdateFrequency;
         LeakFilterQualityOptions = _qualityOptions();
         SelectedLeakFilterQuality = _qualityOptionOrDefault(settings.LocalLeakDatabaseQuality);
      }

      private static bool _persistAndApply()
      {
         _store.Save();
         AppSettings settings = _store.Current;
         bool languageChanged = LocalizationService.ApplyEffective(settings.Language, AppServices.Session.User?.Settings.Language);
         AppServices.Theme?.Apply(ThemePreferences.ResolveEffective(settings.Theme, AppServices.Session.User?.Settings.Theme));
         AppServices.Session.Database?.RefreshAlerts();
         AppServices.Session.Alerts.RefreshHostAlerts();
         return languageChanged;
      }

      private static IReadOnlyList<LeakFilterQualityOption> _qualityOptions()
         =>
         [
            new(LeakFilterQuality.BalancedCode, Strings.Format(nameof(Strings.Label_LeakFilterQuality_Balanced), LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.BalancedRate))),
            new(LeakFilterQuality.StrictCode, Strings.Format(nameof(Strings.Label_LeakFilterQuality_Strict), LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.StrictRate))),
            new(LeakFilterQuality.ParanoidCode, Strings.Format(nameof(Strings.Label_LeakFilterQuality_Paranoid), LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.ParanoidRate))),
         ];

      private static LeakFilterQualityOption _qualityOptionOrDefault(string? code)
      {
         string resolved = LeakFilterQuality.CodeFromRate(LeakFilterQuality.RateFromCode(code));
         return _qualityOptions().First(o => string.Equals(o.Code, resolved, StringComparison.OrdinalIgnoreCase));
      }
   }
}
