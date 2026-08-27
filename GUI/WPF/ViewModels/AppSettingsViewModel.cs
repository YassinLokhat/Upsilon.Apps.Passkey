using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal class AppSettingsViewModel : INotifyPropertyChanged
   {
      public string Title { get; } = AppInfo.Title + " - App Settings";

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

      public event PropertyChangedEventHandler? PropertyChanged;

      public AppSettingsViewModel() { }

      public static void Save()
         => AppInfo.AppSettings.Save(AppInfo.ConfigFile);

      public void Reset()
      {
         AppInfo.AppSettings = new AppSettings();

         DefaultDatabaseDirectory = AppInfo.AppSettings.DefaultDatabaseDirectory;

         Save();
      }
   }
}
