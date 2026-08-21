using System.IO;
using System.Reflection;
using System.Security;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.WPF.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Read-only metadata about the running assembly. Centralises the title shown
   /// in window headers so it is no longer scattered across view-models.
   /// </summary>
   internal static class AppInfo
   {
      private static readonly Lazy<string> _title = new(_buildTitle);
      public static string Title => _title.Value;

      private static readonly Lazy<string> _configFile = new(_buildConfigFile);
      public static string ConfigFile => _configFile.Value;

      public static AppSettings AppSettings { get; set; } = new();

      private static string _buildTitle()
      {
         _ = ConfigFile;

         AssemblyName name = Assembly.GetExecutingAssembly().GetName();
         string version = name.Version?.ToString(3) ?? "0.0.0";
         return $"{name.Name} v{version}";
      }

      private static string _buildConfigFile()
      {
         string configFile = Path.GetFullPath(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "config.json"));

         try
         {
            AppSettings = AppServices.Serialization.Deserialize<AppSettings>(File.ReadAllText(configFile));
         }
         catch (Exception ex)
            when (ex is ArgumentNullException
            or ArgumentException
            or JsonException
            or NotSupportedException
            or PathTooLongException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or FileNotFoundException
            or SecurityException)
         {
            AppSettings.Save(configFile);
            AppServices.Dialogs.Warn("It seems that there is an error in the configuration file.\n", "Config file error");
         }

         return configFile;
      }
   }
}
