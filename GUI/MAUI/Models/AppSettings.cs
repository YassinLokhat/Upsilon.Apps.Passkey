using System.IO;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Models
{
   internal sealed class AppSettings
   {
      public string DefaultDatabaseDirectory { get; set; } = AppPaths.DefaultVaultDirectory;

      public string Language { get; set; } = LocalizationService.SystemCode;

      public string Theme { get; set; } = LocalizationService.SystemCode;

      public LeakFilterConfig LeakFilterConfig { get; set; } = new();

      private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

      public void Save(string configFile)
      {
         string configDirectory = Path.GetDirectoryName(configFile) ?? "./";

         if (!Directory.Exists(configDirectory))
         {
            _ = Directory.CreateDirectory(configDirectory);
         }

         File.WriteAllText(configFile, JsonSerializer.Serialize(this, _options));
      }
   }
}
