using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiAppPaths : IAppPaths
   {
      public MauiAppPaths()
      {
#if WINDOWS
         string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
         bool portable = !string.IsNullOrEmpty(exeDir) && _isWritable(exeDir);
         if (portable)
         {
            ConfigDirectory = exeDir!;
            DefaultDatabaseDirectory = Path.GetFullPath(Path.Join(exeDir, "raw"));
            OfflineLeakFilterPath = Path.GetFullPath(Path.Join(exeDir, "pwned-ntlm.pkbf"));
            CacheDirectory = Path.GetFullPath(Path.Join(exeDir, "cache"));
         }
         else
         {
            ConfigDirectory = FileSystem.AppDataDirectory;
            DefaultDatabaseDirectory = Path.Join(ConfigDirectory, "raw");
            OfflineLeakFilterPath = Path.Join(ConfigDirectory, "pwned-ntlm.pkbf");
            CacheDirectory = FileSystem.CacheDirectory;
         }
#else
         // Android (and future mobile): never use ProcessPath; app-private dirs only.
         ConfigDirectory = FileSystem.AppDataDirectory;
         DefaultDatabaseDirectory = Path.Join(ConfigDirectory, "vaults");
         OfflineLeakFilterPath = null;
         CacheDirectory = FileSystem.CacheDirectory;
#endif
         _ = Directory.CreateDirectory(ConfigDirectory);
         _ = Directory.CreateDirectory(DefaultDatabaseDirectory);
         ConfigFilePath = Path.Join(ConfigDirectory, "config.json");
         LogDirectory = Path.Join(ConfigDirectory, "logs");
         _ = Directory.CreateDirectory(LogDirectory);
      }

      public string ConfigDirectory { get; }
      public string ConfigFilePath { get; }
      public string DefaultDatabaseDirectory { get; }
      public string LogDirectory { get; }
      public string? OfflineLeakFilterPath { get; }
      public string CacheDirectory { get; }

#if WINDOWS
      private static bool _isWritable(string directory)
      {
         try
         {
            string probe = Path.Join(directory, $".passkey-write-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
         }
         catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
         {
            return false;
         }
      }
#endif
   }
}
