namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Resolves writable app directories (config, vaults, logs, optional .pkbf).</summary>
   internal interface IAppPaths
   {
      string ConfigDirectory { get; }

      string ConfigFilePath { get; }

      string DefaultDatabaseDirectory { get; }

      string LogDirectory { get; }

      /// <summary>Offline Bloom path on Windows; <c>null</c> when the host does not support .pkbf (Android).</summary>
      string? OfflineLeakFilterPath { get; }

      string CacheDirectory { get; }
   }
}
