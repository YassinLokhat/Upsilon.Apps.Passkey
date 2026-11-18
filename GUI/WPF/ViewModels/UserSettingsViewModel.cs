using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class UserSettingsViewModel : ObservableObject, ILanguageAware
   {
      public string Title
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildTitle();

      public string Username
      {
         get;
         set => SetProperty(ref field, value);
      } = Strings.Label_NewUser;
      public int LogoutTimeout
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(LogoutTimeoutChecked));
            }
         }
      } = 5;
      public bool LogoutTimeoutChecked
      {
         get => LogoutTimeout != 0;
         set
         {
            if (LogoutTimeoutChecked != value)
            {
               LogoutTimeout = value ? 5 : 0;
            }
         }
      }
      public int CleaningClipboardTimeout
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(CleaningClipboardTimeoutChecked));
            }
         }
      } = 30;
      public bool CleaningClipboardTimeoutChecked
      {
         get => CleaningClipboardTimeout != 0;
         set
         {
            if (CleaningClipboardTimeoutChecked != value)
            {
               CleaningClipboardTimeout = value ? 30 : 0;
            }
         }
      }
      public int ShowPasswordDelay
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(ShowPasswordDelayChecked));
            }
         }
      } = 500;
      public bool ShowPasswordDelayChecked
      {
         get => ShowPasswordDelay != 0;
         set
         {
            if (ShowPasswordDelayChecked != value)
            {
               ShowPasswordDelay = value ? 500 : 0;
            }
         }
      }
      public int NumberOfOldPasswordToKeep
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(NumberOfOldPasswordToKeepChecked));
            }
         }
      }
      public bool NumberOfOldPasswordToKeepChecked
      {
         get => NumberOfOldPasswordToKeep != 0;
         set
         {
            if (NumberOfOldPasswordToKeepChecked != value)
            {
               NumberOfOldPasswordToKeep = value ? 10 : 0;
            }
         }
      }
      public int NumberOfMonthActivitiesToKeep
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               OnPropertyChanged(nameof(NumberOfMonthActivitiesToKeepChecked));
            }
         }
      }
      public bool NumberOfMonthActivitiesToKeepChecked
      {
         get => NumberOfMonthActivitiesToKeep != 0;
         set
         {
            if (NumberOfMonthActivitiesToKeepChecked != value)
            {
               NumberOfMonthActivitiesToKeep = value ? 12 : 0;
            }
         }
      }
      public bool NotifyActivityReview
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyPasswordUpdateReminder
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyDuplicatedPasswords
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyPasswordLeaked
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifySecuritySettings
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyInsufficientPasskeys
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyWeakPasskey
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyPasskeyLeaked
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyWeakAccountPassword
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;
      public bool NotifyPasskeyReusedAsAccountPassword
      {
         get;
         set
         {
            if (SetProperty(ref field, value))
            {
               _previewAlerts();
            }
         }
      } = true;

      public IReadOnlyList<AppLanguage> Languages
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildLanguages();

      public AppLanguage SelectedLanguage
      {
         get;
         set
         {
            if (value is null)
            {
               return;
            }

            // A list rebuilt after a language switch re-selects the same code:
            // only a different pick is previewed.
            AppLanguage previous = field;
            bool picked = previous is null || !string.Equals(previous.Code, value.Code, StringComparison.Ordinal);
            _ = SetProperty(ref field, value);
            if (picked)
            {
               _previewLanguage();
            }
         }
      }

      public IReadOnlyList<AppThemeOption> Themes
      {
         get;
         private set => SetProperty(ref field, value);
      } = _buildThemes();

      public AppThemeOption SelectedTheme
      {
         get;
         set
         {
            if (value is null)
            {
               return;
            }

            AppThemeOption previous = field;
            bool picked = previous is null || !string.Equals(previous.Code, value.Code, StringComparison.Ordinal);
            _ = SetProperty(ref field, value);
            if (picked)
            {
               _previewTheme();
            }
         }
      }

      // Live preview starts once the dialog's values are loaded and stops when it
      // closes. Nothing is written to the vault for a preview: theme and language go
      // through the same effective-value rules as a save, alerts through the
      // broker's preview mask.
      private bool _previewing;

      /// <summary>
      /// Ends the live preview when the dialog closes. Without a save, the
      /// language, theme and alerts shown go back to the saved user settings.
      /// </summary>
      public void EndPreview(bool saved)
      {
         if (!_previewing)
         {
            return;
         }

         _previewing = false;
         AppServices.Session.Alerts.SetPreviewMask(null);
         if (saved)
         {
            return;
         }

         _ = LocalizationService.ApplyEffective(AppInfo.AppSettings.Language, AppServices.Session.User?.Settings.Language);
         _ = ThemeService.ApplyEffective(AppInfo.AppSettings.Theme, AppServices.Session.User?.Settings.Theme);
      }

      private void _previewLanguage()
      {
         if (_previewing)
         {
            _ = LocalizationService.ApplyEffective(AppInfo.AppSettings.Language, SelectedLanguage.Code);
         }
      }

      private void _previewTheme()
      {
         if (_previewing)
         {
            _ = ThemeService.ApplyEffective(AppInfo.AppSettings.Theme, SelectedTheme.Code);
         }
      }

      private void _previewAlerts()
      {
         if (_previewing && AppServices.Session.User is not null)
         {
            AppServices.Session.Alerts.SetPreviewMask(BuildAlertsToNotify());
         }
      }

      public UserSettingsViewModel()
      {
         SelectedLanguage = _languageFromSettings(
            AppServices.Session.Database?.User?.Settings.Language);
         SelectedTheme = _themeFromSettings(
            AppServices.Session.Database?.User?.Settings.Theme);

         if (AppServices.Session.Database?.User is not { } user)
         {
            _previewing = true;
            return;
         }

         Username = user.Username;

         LogoutTimeout = user.Settings.LogoutTimeout;
         CleaningClipboardTimeout = user.Settings.CleaningClipboardTimeout;
         ShowPasswordDelay = user.Settings.ShowPasswordDelay;
         NumberOfOldPasswordToKeep = user.Settings.NumberOfOldPasswordToKeep;
         NumberOfMonthActivitiesToKeep = user.Settings.NumberOfMonthActivitiesToKeep;

         AlertKindList notify = user.Settings.AlertsToNotify;
         NotifyActivityReview = notify.Contains(AlertKinds.ActivityReview);
         NotifyPasswordUpdateReminder = notify.Contains(AlertKinds.PasswordUpdateReminder);
         NotifyDuplicatedPasswords = notify.Contains(AlertKinds.DuplicatedPasswords);
         NotifyPasswordLeaked = notify.Contains(AlertKinds.PasswordLeaked);
         NotifySecuritySettings = notify.Contains(AlertKinds.VaultSecuritySettings)
            || notify.Contains(AlertKinds.HostSecuritySettings);
         NotifyInsufficientPasskeys = notify.Contains(AlertKinds.InsufficientPasskeys);
         NotifyWeakPasskey = notify.Contains(AlertKinds.WeakPasskey);
         NotifyPasskeyLeaked = notify.Contains(AlertKinds.PasskeyLeaked);
         NotifyWeakAccountPassword = notify.Contains(AlertKinds.WeakAccountPassword);
         NotifyPasskeyReusedAsAccountPassword = notify.Contains(AlertKinds.PasskeyReusedAsAccountPassword);
         _previewing = true;
      }

      public AlertKindList BuildAlertsToNotify()
      {
         List<string> kinds = [];

         if (NotifyActivityReview)
         {
            kinds.Add(AlertKinds.ActivityReview);
         }

         if (NotifyDuplicatedPasswords)
         {
            kinds.Add(AlertKinds.DuplicatedPasswords);
         }

         if (NotifyPasswordUpdateReminder)
         {
            kinds.Add(AlertKinds.PasswordUpdateReminder);
         }

         if (NotifyPasswordLeaked)
         {
            kinds.Add(AlertKinds.PasswordLeaked);
         }

         if (NotifySecuritySettings)
         {
            kinds.Add(AlertKinds.VaultSecuritySettings);
            kinds.Add(AlertKinds.HostSecuritySettings);
         }

         if (NotifyInsufficientPasskeys)
         {
            kinds.Add(AlertKinds.InsufficientPasskeys);
         }

         if (NotifyWeakPasskey)
         {
            kinds.Add(AlertKinds.WeakPasskey);
         }

         if (NotifyPasskeyLeaked)
         {
            kinds.Add(AlertKinds.PasskeyLeaked);
         }

         if (NotifyWeakAccountPassword)
         {
            kinds.Add(AlertKinds.WeakAccountPassword);
         }

         if (NotifyPasskeyReusedAsAccountPassword)
         {
            kinds.Add(AlertKinds.PasskeyReusedAsAccountPassword);
         }

         return new AlertKindList(kinds);
      }

      public void OnLanguageChanged()
      {
         string languageCode = SelectedLanguage.Code;
         string themeCode = SelectedTheme.Code;
         Title = _buildTitle();
         Languages = _buildLanguages();
         Themes = _buildThemes();
         SelectedLanguage = _languageFromSettings(languageCode);
         SelectedTheme = _themeFromSettings(themeCode);
      }

      private static string _buildTitle()
         => AppServices.Session.Database?.User is null
            ? Strings.Format(nameof(Strings.Title_NewUser), AppInfo.Title)
            : Strings.Format(nameof(Strings.Title_UserSettings), AppInfo.Title);

      private static IReadOnlyList<AppLanguage> _buildLanguages()
         =>
         [
            new(string.Empty, Strings.Label_UseAppLanguage),
            .. LocalizationService.Supported,
         ];

      private static IReadOnlyList<AppThemeOption> _buildThemes()
         =>
         [
            new(string.Empty, Strings.Label_UseAppTheme),
            .. ThemeService.Supported,
         ];

      private AppLanguage _languageFromSettings(string? code)
      {
         return string.IsNullOrWhiteSpace(code)
            ? Languages[0]
            : Languages.FirstOrDefault(l =>
            string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? Languages[0];
      }

      private AppThemeOption _themeFromSettings(string? code)
      {
         return string.IsNullOrWhiteSpace(code)
            ? Themes[0]
            : Themes.FirstOrDefault(t =>
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? Themes[0];
      }
   }
}
