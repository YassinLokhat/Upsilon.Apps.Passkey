using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class AppSettingsViewModel : INotifyPropertyChanged, ILanguageAware
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_AppSettings), AppInfo.Title);

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

      public void OnLanguageChanged()
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));

      /// <summary>
      /// Persists settings and applies the UI culture. Returns <see langword="true"/>
      /// when the language code changed (open windows are refreshed in place).
      /// </summary>
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called on the bound ViewModel instance from the view.")]
      public bool Save()
      {
         AppInfo.AppSettings.Save(AppInfo.ConfigFile);
         return LocalizationService.Apply(AppInfo.AppSettings.Language);
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
