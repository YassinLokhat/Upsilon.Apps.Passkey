using System.IO;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   /// <summary>
   /// Platform-aware paths for config, vaults, and logs.
   /// Windows: beside the executable (like WPF). Android: app data directory.
   /// </summary>
   internal static class AppPaths
   {
      public static string AppRoot
      {
         get
         {
#if ANDROID
            return FileSystem.AppDataDirectory;
#else
            string? processDir = Path.GetDirectoryName(Environment.ProcessPath);
            return string.IsNullOrEmpty(processDir)
               ? FileSystem.AppDataDirectory
               : processDir;
#endif
         }
      }

      public static string ConfigFile => Path.Join(AppRoot, "config.json");

      public static string DefaultVaultDirectory => Path.Join(AppRoot, "raw");

      public static string LogsDirectory
      {
         get
         {
#if ANDROID
            return Path.Join(AppRoot, "logs");
#else
            return Path.Join(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "Passkey",
               "logs");
#endif
         }
      }

      public static string VaultPathForUsername(string usernameHash)
         => Path.GetFullPath(Path.Join(DefaultVaultDirectory, $"{usernameHash}.pku"));
   }
}
