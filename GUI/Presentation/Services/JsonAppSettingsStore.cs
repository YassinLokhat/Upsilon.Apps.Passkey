using System.Security;
using System.Text.Json;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   internal sealed class JsonAppSettingsStore(IAppPaths paths) : IAppSettingsStore
   {
      private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, };

      private readonly IAppPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));

      public AppSettings Current { get; set; } = _withHostDefaults(new AppSettings(), paths);

      public string ConfigFilePath => _paths.ConfigFilePath;

      public bool Load()
      {
         try
         {
            AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(ConfigFilePath));
            if (loaded is null)
            {
               throw new JsonException("Empty configuration file.");
            }

            Current = _withHostDefaults(loaded, _paths);
            return true;
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or JsonException
            or NotSupportedException
            or IOException
            or UnauthorizedAccessException
            or SecurityException)
         {
            AppServices.Log.Error("App settings missing or invalid; writing defaults", ex);
            Current = CreateDefault();
            _trySave();
            return false;
         }
      }

      public void Save()
      {
         string configDirectory = Path.GetDirectoryName(ConfigFilePath) ?? "./";
         _ = Directory.CreateDirectory(configDirectory);
         File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(Current, _options));
      }

      public AppSettings CreateDefault() => _withHostDefaults(new AppSettings(), _paths);

      private void _trySave()
      {
         try
         {
            Save();
         }
         catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
         {
            AppServices.Log.Error("Could not write default app settings", ex);
         }
      }

      private static AppSettings _withHostDefaults(AppSettings settings, IAppPaths paths)
      {
         if (string.IsNullOrWhiteSpace(settings.DefaultDatabaseDirectory))
         {
            settings.DefaultDatabaseDirectory = paths.DefaultDatabaseDirectory;
         }

         if (paths.OfflineLeakFilterPath is { } filterPath)
         {
            settings.LeakFilterConfig.FilterPath = filterPath;
         }
         else
         {
            settings.LeakFilterConfig.FilterPath = string.Empty;
            settings.LocalLeakDatabaseEnabled = false;
         }

         return settings;
      }
   }
}
