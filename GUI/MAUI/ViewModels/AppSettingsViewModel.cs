using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Input;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;
using Upsilon.Apps.Passkey.GUI.MAUI.Themes;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels
{
   internal sealed class AppSettingsViewModel : ObservableObject
   {
      public AppSettingsViewModel()
      {
         LanguageCodes = [.. LocalizationService.Shipped.Select(s => s.Code)];
         ThemeCodes = [ThemeService.SystemCode, ThemeService.LightCode, ThemeService.DarkCode];
         SelectedLanguage = PasskeyAppInfo.AppSettings.Language;
         SelectedTheme = PasskeyAppInfo.AppSettings.Theme;
         DefaultDatabaseDirectory = PasskeyAppInfo.AppSettings.DefaultDatabaseDirectory;
         RefreshOfflineLeakFilterStatus();

         SaveCommand = new AsyncRelayCommand(_saveAsync);
         ResetCommand = new AsyncRelayCommand(_resetAsync);
         BrowseCommand = new AsyncRelayCommand(_browseAsync);
         BuildLeakFilterCommand = new AsyncRelayCommand(_buildLeakFilterAsync, () => !OfflineLeakFilterBusy);
         DeleteLeakFilterCommand = new AsyncRelayCommand(_deleteLeakFilterAsync, () => !OfflineLeakFilterBusy);
         BackCommand = new AsyncRelayCommand(() => AppServices.Navigation.GoBackAsync());
      }

      public string Title => Strings.Format(nameof(Strings.Title_AppSettings), PasskeyAppInfo.Title);

      public IReadOnlyList<string> LanguageCodes { get; }

      public IReadOnlyList<string> ThemeCodes { get; }

      public string SelectedLanguage
      {
         get;
         set => SetProperty(ref field, value);
      } = LocalizationService.SystemCode;

      public string SelectedTheme
      {
         get;
         set => SetProperty(ref field, value);
      } = ThemeService.SystemCode;

      public string DefaultDatabaseDirectory
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public bool OfflineLeakFilterEnabled
      {
         get;
         set
         {
            if (SetProperty(ref field, value) && !OfflineLeakFilterBusy)
            {
               PasskeyAppInfo.AppSettings.LeakFilterConfig.Enabled = value;
               if (AppServices.PasswordFactory is PasswordFactory factory)
               {
                  factory.ReloadLocalFilter(PasskeyAppInfo.AppSettings.LeakFilterConfig);
               }

               RefreshOfflineLeakFilterStatus();
            }
         }
      }

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
               if (BuildLeakFilterCommand is AsyncRelayCommand build)
               {
                  build.NotifyCanExecuteChanged();
               }

               if (DeleteLeakFilterCommand is AsyncRelayCommand del)
               {
                  del.NotifyCanExecuteChanged();
               }
            }
         }
      }

      public bool OfflineLeakFilterIdle => !OfflineLeakFilterBusy;

      public string OfflineLeakFilterProgress
      {
         get;
         set => SetProperty(ref field, value);
      } = string.Empty;

      public ICommand SaveCommand { get; }
      public ICommand ResetCommand { get; }
      public ICommand BrowseCommand { get; }
      public ICommand BuildLeakFilterCommand { get; }
      public ICommand DeleteLeakFilterCommand { get; }
      public ICommand BackCommand { get; }

      public void RefreshOfflineLeakFilterStatus()
      {
         OfflineLeakFilterEnabled = PasskeyAppInfo.AppSettings.LeakFilterConfig.Enabled;
         string path = PasskeyAppInfo.AppSettings.LeakFilterConfig.FilterPath;

         if (!File.Exists(path))
         {
            OfflineLeakFilterStatus = Strings.Msg_OfflineLeakFileAbsent;
            return;
         }

         FileInfo info = new(path);
         double sizeGiB = info.Length / (1024d * 1024d * 1024d);
         string updated = info.LastWriteTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " UTC";
         OfflineLeakFilterStatus = PasskeyAppInfo.AppSettings.LeakFilterConfig.Enabled
            ? Strings.Format(nameof(Strings.Msg_OfflineLeakFilePresent), sizeGiB, updated)
            : Strings.Format(nameof(Strings.Msg_OfflineLeakFilePresentDisabled), sizeGiB, updated);
      }

      private async Task _saveAsync()
      {
         if (Directory.Exists(DefaultDatabaseDirectory))
         {
            PasskeyAppInfo.AppSettings.DefaultDatabaseDirectory = DefaultDatabaseDirectory;
         }

         PasskeyAppInfo.AppSettings.Language = SelectedLanguage;
         PasskeyAppInfo.AppSettings.Theme = SelectedTheme;
         PasskeyAppInfo.AppSettings.Save(PasskeyAppInfo.ConfigFile);
         _ = LocalizationService.ApplyEffective(
            PasskeyAppInfo.AppSettings.Language,
            AppServices.Session.User?.Settings.Language);
         _ = ThemeService.ApplyEffective(
            PasskeyAppInfo.AppSettings.Theme,
            AppServices.Session.User?.Settings.Theme);
         OnPropertyChanged(nameof(Title));
         await AppServices.Dialogs.InfoAsync(Strings.Msg_Saved, Strings.Title_Success).ConfigureAwait(true);
      }

      private async Task _resetAsync()
      {
         PasskeyAppInfo.AppSettings = new Models.AppSettings();
         DefaultDatabaseDirectory = PasskeyAppInfo.AppSettings.DefaultDatabaseDirectory;
         SelectedLanguage = PasskeyAppInfo.AppSettings.Language;
         SelectedTheme = PasskeyAppInfo.AppSettings.Theme;
         RefreshOfflineLeakFilterStatus();
         await _saveAsync().ConfigureAwait(true);
      }

      private async Task _browseAsync()
      {
         string? folder = await AppServices.Dialogs
            .PickFolderAsync(Strings.Title_BrowseDatabaseDirectory, DefaultDatabaseDirectory)
            .ConfigureAwait(true);
         if (folder is not null)
         {
            DefaultDatabaseDirectory = folder;
         }
      }

      private async Task _buildLeakFilterAsync()
      {
         if (OfflineLeakFilterBusy)
         {
            return;
         }

         bool update = File.Exists(PasskeyAppInfo.AppSettings.LeakFilterConfig.FilterPath);
         bool ok = await AppServices.Dialogs.ConfirmAsync(
            update ? Strings.Msg_UpdateOfflineLeakDatabase : Strings.Msg_BuildOfflineLeakDatabase,
            update ? Strings.Title_UpdateOfflineLeakDatabase : Strings.Title_BuildOfflineLeakDatabase)
            .ConfigureAwait(true);
         if (!ok)
         {
            return;
         }

         OfflineLeakFilterBusy = true;
         OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildStarting;

         if (AppServices.PasswordFactory is PasswordFactory detaching)
         {
            detaching.AttachLocalFilter(null);
         }

         try
         {
            Progress<HibpBloomBuildProgress> progress = new(p =>
            {
               if (p.Skipped)
               {
                  OfflineLeakFilterProgress = Strings.Msg_OfflineLeakBuildSkipped;
                  return;
               }

               double pct = 100.0 * p.CompletedPrefixes / p.TotalPrefixes;
               OfflineLeakFilterProgress = p.IsRefresh
                  ? Strings.Format(
                     nameof(Strings.Msg_OfflineLeakUpdateProgress),
                     pct,
                     p.CompletedPrefixes,
                     p.TotalPrefixes,
                     p.UnchangedPrefixes,
                     p.ChangedPrefixes,
                     p.DownloadedBytes / (1024.0 * 1024.0))
                  : Strings.Format(
                     nameof(Strings.Msg_OfflineLeakBuildProgress),
                     pct,
                     p.CompletedPrefixes,
                     p.TotalPrefixes,
                     p.InsertedHashes);
            });

            string filterPath = PasskeyAppInfo.AppSettings.LeakFilterConfig.FilterPath;
            HibpBloomBuildResult result = await HibpBloomBuilder.RunAsync(
               filterPath,
               update ? HibpBloomBuildMode.Update : HibpBloomBuildMode.BuildIfMissing,
               progress: progress).ConfigureAwait(true);

            PasskeyAppInfo.AppSettings.LeakFilterConfig.Enabled = true;
            OfflineLeakFilterEnabled = true;
            OfflineLeakFilterProgress = result.Skipped
               ? Strings.Msg_OfflineLeakAlreadyUpToDate
               : result.IsRefresh
               ? Strings.Format(
                  nameof(Strings.Msg_OfflineLeakUpdateComplete),
                  result.ChangedPrefixes,
                  result.UnchangedPrefixes,
                  result.DownloadedBytes / (1024.0 * 1024.0))
               : Strings.Format(nameof(Strings.Msg_OfflineLeakBuildComplete), result.InsertedCount);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or HttpRequestException
            or IOException
            or UnauthorizedAccessException
            or OperationCanceledException)
         {
            await AppServices.Dialogs.WarnAsync(
               Strings.Format(nameof(Strings.Msg_OfflineLeakBuildFailed), ex.Message),
               Strings.Title_BuildFailed).ConfigureAwait(true);
            OfflineLeakFilterProgress = Strings.Msg_BuildFailed;
         }
         finally
         {
            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.ReloadLocalFilter(PasskeyAppInfo.AppSettings.LeakFilterConfig);
            }

            RefreshOfflineLeakFilterStatus();
            OfflineLeakFilterBusy = false;
         }
      }

      private async Task _deleteLeakFilterAsync()
      {
         string path = PasskeyAppInfo.AppSettings.LeakFilterConfig.FilterPath;
         if (!File.Exists(path))
         {
            await AppServices.Dialogs.InfoAsync(Strings.Msg_NoOfflineLeakDatabase, Strings.Title_OfflineLeakDatabase)
               .ConfigureAwait(true);
            return;
         }

         bool ok = await AppServices.Dialogs
            .ConfirmAsync(Strings.Msg_DeleteOfflineLeakDatabase, Strings.Title_DeleteOfflineLeakDatabase)
            .ConfigureAwait(true);
         if (!ok)
         {
            return;
         }

         try
         {
            if (AppServices.PasswordFactory is PasswordFactory factory)
            {
               factory.AttachLocalFilter(null);
            }

            File.Delete(path);
            PasskeyAppInfo.AppSettings.LeakFilterConfig.Enabled = false;
            OfflineLeakFilterEnabled = false;
            RefreshOfflineLeakFilterStatus();
         }
         catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException)
         {
            await AppServices.Dialogs.WarnAsync(ex.Message, Strings.Title_Error).ConfigureAwait(true);
         }
      }
   }
}
