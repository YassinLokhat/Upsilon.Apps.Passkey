using System.IO;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.Models
{
   internal class AppSettings
   {
      public string DefaultDatabaseDirectory { get; set; } = Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "raw"));

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
