using System.ComponentModel;
using System.IO;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class AppSettingsViewModel : INotifyPropertyChanged
   {
      private readonly string _languageWhenOpened;

      public string Title { get; } = Strings.Format(nameof(Strings.Title_AppSettings), AppInfo.Title);

      public IReadOnlyList<AppLanguage> Languages { get; } = LocalizationService.Supported;

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

      public event PropertyChangedEventHandler? PropertyChanged;

      public AppSettingsViewModel()
      {
         _languageWhenOpened = AppInfo.AppSettings.Language;
      }

      /// <summary>
      /// Persists settings and applies the UI culture. Returns <see langword="true"/>
      /// when the language code changed (caller should tell the user to restart).
      /// </summary>
      public bool Save()
      {
         AppInfo.AppSettings.Save(AppInfo.ConfigFile);
         LocalizationService.Apply(AppInfo.AppSettings.Language);
         return !string.Equals(_languageWhenOpened, AppInfo.AppSettings.Language, StringComparison.OrdinalIgnoreCase);
      }

      public void Reset()
      {
         AppInfo.AppSettings = new AppSettings();

         DefaultDatabaseDirectory = AppInfo.AppSettings.DefaultDatabaseDirectory;
         SelectedLanguage = LocalizationService.GetLanguageOrDefault(AppInfo.AppSettings.Language);

         _ = Save();
      }
   }
}
