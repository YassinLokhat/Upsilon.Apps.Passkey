using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>Direct filesystem vault (Windows / tests). Write-back is a no-op when path == working path.</summary>
   internal sealed class PathVaultSource : IVaultSource
   {
      public PathVaultSource(string path)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         WorkingPath = Path.GetFullPath(path);
         DisplayName = Path.GetFileName(WorkingPath);
      }

      public string WorkingPath { get; }

      public string DisplayName { get; }

      public Task CheckoutAsync(CancellationToken cancellationToken = default)
         => Task.CompletedTask;

      public Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default)
         => Task.FromResult(VaultWriteBackResult.Skipped);
   }

   internal sealed class PathVaultSourceFactory : IVaultSourceFactory
   {
      public Task<IVaultSource> OpenPathAsync(string path, CancellationToken cancellationToken = default)
         => Task.FromResult<IVaultSource>(new PathVaultSource(path));

      public Task<IVaultSource> CreatePathAsync(string path, CancellationToken cancellationToken = default)
      {
         string full = Path.GetFullPath(path);
         string? dir = Path.GetDirectoryName(full);
         if (!string.IsNullOrEmpty(dir))
         {
            _ = Directory.CreateDirectory(dir);
         }

         return Task.FromResult<IVaultSource>(new PathVaultSource(full));
      }
   }
}
