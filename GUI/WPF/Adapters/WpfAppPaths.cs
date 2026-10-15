using System.IO;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   /// <summary>
   /// Portable layout when the exe directory is writable; otherwise LocalAppData.
   /// </summary>
   internal sealed class WpfAppPaths : IAppPaths
   {
      public WpfAppPaths()
      {
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
            ConfigDirectory = Path.Join(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "Passkey");
            DefaultDatabaseDirectory = Path.Join(ConfigDirectory, "raw");
            OfflineLeakFilterPath = Path.Join(ConfigDirectory, "pwned-ntlm.pkbf");
            CacheDirectory = Path.Join(ConfigDirectory, "cache");
            _ = Directory.CreateDirectory(ConfigDirectory);
         }

         ConfigFilePath = Path.Join(ConfigDirectory, "config.json");
         LogDirectory = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Passkey",
            "logs");
      }

      public string ConfigDirectory { get; }
      public string ConfigFilePath { get; }
      public string DefaultDatabaseDirectory { get; }
      public string LogDirectory { get; }
      public string? OfflineLeakFilterPath { get; }
      public string CacheDirectory { get; }

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
            Log.Error(ex, "Exe directory is not writable; using LocalAppData paths");
            return false;
         }
      }
   }
}
