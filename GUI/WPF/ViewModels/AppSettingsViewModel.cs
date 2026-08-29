using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class AppSettingsViewModel : INotifyPropertyChanged, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_AppSettings), AppInfo.Title);

      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh the System language label on language change.")]
      public IReadOnlyList<AppLanguage> Languages => LocalizationService.Supported;

      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh theme labels on language change.")]
      public IReadOnlyList<AppThemeOption> Themes => ThemeService.Supported;

      public string DefaultDatabaseDirectory
      {
         get;
         set
         {
            if (!Directory.Exists(value))
            {
               return;
            }

            _ = PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
            AppInfo.AppSettings.DefaultDatabaseDirectory = field;
         }
      } = AppInfo.AppSettings.DefaultDatabaseDirectory;

      public AppLanguage SelectedLanguage
      {
         get;
         set
         {
            if (value is null)
            {
               return;
            }

            _ = PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
            AppInfo.AppSettings.Language = field.Code;
         }
      } = LocalizationService.GetLanguageOrDefault(AppInfo.AppSettings.Language);

      public AppThemeOption SelectedTheme
      {
         get;
         set
         {
            if (value is null)
            {
               return;
            }

            _ = PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
            AppInfo.AppSettings.Theme = field.Code;
         }
      } = ThemeService.GetOptionOrDefault(AppInfo.AppSettings.Theme);

      // --- Application-level offline leak filter (leak-filter.json next to exe) ---

      public bool OfflineLeakFilterEnabled
      {
         get;
         set
         {
            if (field == value)
            {
               return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OfflineLeakFilterEnabled)));
         }
      }

      public string OfflineLeakFilterStatus
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = "Unknown";

      public bool OfflineLeakFilterBusy
      {
         get;
         set
         {
            if (field == value)
            {
               return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OfflineLeakFilterBusy)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OfflineLeakFilterIdle)));
         }
      }

      public bool OfflineLeakFilterIdle => !OfflineLeakFilterBusy;

      public string OfflineLeakFilterProgress
      {
         get;
         set => PropertyHelper.SetProperty(ref field, value, this, PropertyChanged);
      } = string.Empty;

      public event PropertyChangedEventHandler? PropertyChanged;

      public AppSettingsViewModel()
      {
         RefreshOfflineLeakFilterStatus();
      }

      public void OnLanguageChanged()
      {
         string languageCode = SelectedLanguage.Code;
         string themeCode = SelectedTheme.Code;
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Languages)));
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Themes)));
         SelectedLanguage = LocalizationService.GetLanguageOrDefault(languageCode);
         SelectedTheme = ThemeService.GetOptionOrDefault(themeCode);
      }

      /// <summary>
      /// Persists settings and applies the effective UI culture and theme (user
      /// override when a session is open, otherwise the app values).
      /// Returns <see langword="true"/> when the culture code changed.
      /// </summary>
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called on the bound ViewModel instance from the view.")]
      public bool Save()
      {
         AppInfo.AppSettings.Save(AppInfo.ConfigFile);
         bool languageChanged = LocalizationService.ApplyEffective(
            AppInfo.AppSettings.Language,
            AppServices.Session.User?.Settings.Language);
         _ = ThemeService.ApplyEffective(
            AppInfo.AppSettings.Theme,
            AppServices.Session.User?.Settings.Theme);
         return languageChanged;
      }

      public void Reset()
      {
         AppInfo.AppSettings = new AppSettings();

         DefaultDatabaseDirectory = AppInfo.AppSettings.DefaultDatabaseDirectory;
         SelectedLanguage = LocalizationService.GetLanguageOrDefault(AppInfo.AppSettings.Language);
         SelectedTheme = ThemeService.GetOptionOrDefault(AppInfo.AppSettings.Theme);

         _ = Save();
      }

      public void RefreshOfflineLeakFilterStatus()
      {
         OfflineLeakFilterEnabled = AppInfo.AppSettings.LeakFilterConfig.Enabled;

         string path = AppInfo.AppSettings.LeakFilterConfig.FilterPath;

         if (!File.Exists(path))
         {
            OfflineLeakFilterStatus = $"Absent file {path}";
            return;
         }

         FileInfo info = new(path);
         string size = $"{info.Length / (1024d * 1024d * 1024d):0.00} GiB";
         string updated = info.LastWriteTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " UTC";
         OfflineLeakFilterStatus = AppInfo.AppSettings.LeakFilterConfig.Enabled
            ? $"Present · {size} · updated {updated} · {path}"
            : $"Present on disk · {size} · updated {updated} · disabled · {path}";
      }
   }
}
