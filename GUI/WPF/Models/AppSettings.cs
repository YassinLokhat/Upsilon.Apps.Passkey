using System.IO;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.GUI.WPF.Models
{
   internal class AppSettings
   {
      public string DefaultDatabaseDirectory { get; set; } = Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "raw"));

      /// <summary>
      /// UI language preference (<c>System</c>, <c>en</c>, <c>fr</c>, …). Machine-wide;
      /// not stored in the vault. Users may override it in their vault settings.
      /// <c>System</c> follows the OS UI language when a satellite ships.
      /// </summary>
      public string Language { get; set; } = LocalizationService.SystemCode;

      /// <summary>
      /// UI theme preference (<c>System</c>, <c>Light</c>, <c>Dark</c>). Machine-wide;
      /// not stored in the vault. Users may override it in their vault settings.
      /// </summary>
      public string Theme { get; set; } = LocalizationService.SystemCode;

      public LeakFilterConfig LeakFilterConfig { get; set; } = new();

      private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, };
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
