using System.Text.Json;
using System.Text.Json.Serialization;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Application-level paths and config for the optional offline HIBP Bloom filter
   /// (not inside the encrypted vault). Shared by all vault users: enable/disable
   /// never deletes the <c>.pkbf</c>. The root directory defaults to
   /// <c>%LocalAppData%\Passkey</c> and can be redirected via
   /// <see cref="SetRootDirectory"/> (e.g. a folder next to the GUI exe).
   /// </summary>
   public static class LeakFilterPaths
   {
      public const string FilterFileName = "pwned-sha1.pkbf";
      public const string ConfigFileName = "leak-filter.json";

      private static string _rootDirectory = _defaultRootDirectory();

      /// <summary>
      /// Directory that holds <see cref="FilterFileName"/> and <see cref="ConfigFileName"/>.
      /// </summary>
      public static string RootDirectory => _rootDirectory;

      public static string FilterFilePath => Path.Combine(RootDirectory, FilterFileName);

      public static string ConfigFilePath => Path.Combine(RootDirectory, ConfigFileName);

      /// <summary>
      /// Redirects where the filter and config files live. Call before opening or
      /// building the filter (typically once at process start).
      /// </summary>
      public static void SetRootDirectory(string rootDirectory)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
         _rootDirectory = Path.GetFullPath(rootDirectory);
      }

      /// <summary>
      /// Absolute path of the <c>.pkbf</c> to use: <see cref="LeakFilterConfig.FilterPath"/>
      /// when set, otherwise <see cref="FilterFilePath"/>.
      /// </summary>
      public static string ResolveFilterFilePath(LeakFilterConfig? config = null)
      {
         config ??= LoadConfig();
         return string.IsNullOrWhiteSpace(config.FilterPath)
            ? FilterFilePath
            : Path.GetFullPath(config.FilterPath);
      }

      /// <summary>
      /// Loads whether the offline filter should be used. Defaults to enabled when
      /// the config file is missing (use the filter if the <c>.pkbf</c> exists).
      /// </summary>
      public static LeakFilterConfig LoadConfig()
      {
         try
         {
            if (!File.Exists(ConfigFilePath))
            {
               return LeakFilterConfig.Default;
            }

            string json = File.ReadAllText(ConfigFilePath);
            LeakFilterConfig? config = JsonSerializer.Deserialize(json, LeakFilterConfigJsonContext.Default.LeakFilterConfig);
            return config ?? LeakFilterConfig.Default;
         }
#pragma warning disable CA1031 // Config load must never crash callers
         catch
#pragma warning restore CA1031
         {
            return LeakFilterConfig.Default;
         }
      }

      public static void SaveConfig(LeakFilterConfig config)
      {
         ArgumentNullException.ThrowIfNull(config);
         _ = Directory.CreateDirectory(RootDirectory);
         string json = JsonSerializer.Serialize(config, LeakFilterConfigJsonContext.Default.LeakFilterConfig);
         File.WriteAllText(ConfigFilePath, json);
      }

      /// <summary>
      /// Deletes the resolved <c>.pkbf</c> when present. Does not change <see cref="LeakFilterConfig.Enabled"/>.
      /// </summary>
      public static bool TryDeleteFilterFile()
      {
         string path = ResolveFilterFilePath();
         if (!File.Exists(path))
         {
            return false;
         }

         File.Delete(path);
         return true;
      }

      /// <summary>
      /// Opens the configured filter when enabled and present; otherwise returns <see langword="null"/>.
      /// </summary>
      public static ILocalLeakFilter? TryOpenConfiguredFilter()
      {
         LeakFilterConfig config = LoadConfig();
         if (!config.Enabled)
         {
            return null;
         }

         string path = ResolveFilterFilePath(config);
         if (!File.Exists(path))
         {
            return null;
         }

         try
         {
            return HibpBloomFile.Open(path);
         }
#pragma warning disable CA1031 // A corrupt filter must not block password generation
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"Offline leak filter could not be opened: {ex}");
            return null;
         }
      }

      private static string _defaultRootDirectory()
         => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Passkey");
   }

   /// <summary>
   /// Application-level leak-filter preferences (not stored in the vault).
   /// </summary>
   public sealed class LeakFilterConfig
   {
      public static LeakFilterConfig Default { get; } = new();

      /// <summary>
      /// When <see langword="true"/> and the <c>.pkbf</c> exists, use it after HIBP/XON fail.
      /// Disabling never deletes the on-disk filter.
      /// </summary>
      public bool Enabled { get; set; } = true;

      /// <summary>
      /// Optional absolute override path for the <c>.pkbf</c>; empty means
      /// <see cref="LeakFilterPaths.FilterFilePath"/> under the current root.
      /// </summary>
      public string FilterPath { get; set; } = string.Empty;
   }

   [JsonSerializable(typeof(LeakFilterConfig))]
   internal sealed partial class LeakFilterConfigJsonContext : JsonSerializerContext
   {
   }
}
