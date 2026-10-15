using System.Globalization;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings
{
   /// <summary>
   /// Application settings. Edits stay in this ViewModel until <see cref="Save"/>,
   /// so closing without saving changes nothing (including the login idle timeout).
   /// The offline leak filter section is only meaningful when
   /// <see cref="IHostCapabilities.SupportsOfflineLeakFilter"/> is set.
   /// </summary>
   internal sealed class AppSettingsViewModel : ObservableObject, ILanguageRefreshable
   {
      public AppSettingsViewModel()
      {
         _loadFrom(_store.Current);
         RefreshOfflineLeakFilterStatus();
      }

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
