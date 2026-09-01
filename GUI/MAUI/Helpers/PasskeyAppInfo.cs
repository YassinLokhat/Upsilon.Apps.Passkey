using System.IO;
using System.Reflection;
using System.Security;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.MAUI.Models;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   /// <summary>
   /// Passkey app metadata and config.json loader (named to avoid clashing with
   /// <c>Microsoft.Maui.ApplicationModel.AppInfo</c>).
   /// </summary>
   internal static class PasskeyAppInfo
   {
      private static readonly Lazy<string> _title = new(_buildTitle);
      public static string Title => _title.Value;

      private static readonly Lazy<string> _configFile = new(_buildConfigFile);
      public static string ConfigFile => _configFile.Value;

      public static AppSettings AppSettings { get; set; } = new();

      public static bool ConfigLoadHadError { get; private set; }

      public static bool TryConsumeConfigLoadError()
      {
         if (!ConfigLoadHadError)
         {
            return false;
         }

         ConfigLoadHadError = false;
         return true;
      }

      private static string _buildTitle()
      {
         _ = ConfigFile;
         AssemblyName name = Assembly.GetExecutingAssembly().GetName();
         string version = name.Version?.ToString(3) ?? "0.0.0";
         return $"Passkey v{version}";
      }

      private static string _buildConfigFile()
      {
         string configFile = AppPaths.ConfigFile;

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
            if (string.IsNullOrEmpty(AppSettings.DefaultDatabaseDirectory))
            {
               AppSettings.DefaultDatabaseDirectory = AppPaths.DefaultVaultDirectory;
            }

            AppSettings.Save(configFile);
            ConfigLoadHadError = true;
         }

         if (AppServices.PasswordFactory is PasswordFactory factory)
         {
            factory.ReloadLocalFilter(AppSettings.LeakFilterConfig);
         }

         return configFile;
      }
   }
}
