using System.Text.RegularExpressions;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   /// <summary>Direct filesystem vault (Windows / tests). Write-back is a no-op when path == working path.</summary>
   internal sealed partial class PathVaultSource : IVaultSource
   {
      public PathVaultSource(string path)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(path);
         WorkingPath = Path.GetFullPath(path);
      }

      public string WorkingPath { get; private set; }

      public string DisplayName => Path.GetFileName(WorkingPath);

      public string Location => WorkingPath;

      public Task<VaultCheckoutResult> CheckoutAsync(CancellationToken cancellationToken = default)
         => Task.FromResult(VaultCheckoutResult.Fresh);

      public Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default)
         => Task.FromResult(VaultWriteBackResult.Skipped);

      /// <summary>
      /// Core already removed the vault and its lock; this clears the atomic
      /// replace leftovers of this exact vault name only.
      /// </summary>
      public Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
      {
         if (File.Exists(WorkingPath))
         {
            return Task.FromResult(false);
         }

         foreach (string leftover in ReplaceLeftovers(WorkingPath))
         {
            File.Delete(leftover);
         }

         return Task.FromResult(true);
      }

      public Task<bool> RenameAsync(string newFileName, CancellationToken cancellationToken = default)
      {
         if (!IsPlainFileName(newFileName) || !File.Exists(WorkingPath))
         {
            return Task.FromResult(false);
         }

         string directory = Path.GetDirectoryName(WorkingPath) ?? string.Empty;
         string target = Path.Join(directory, newFileName);
         if (File.Exists(target) || File.Exists($"{target}.lock"))
         {
            return Task.FromResult(false);
         }

         File.Move(WorkingPath, target, overwrite: false);
         WorkingPath = Path.GetFullPath(target);
         return Task.FromResult(true);
      }

      public ValueTask DisposeAsync() => ValueTask.CompletedTask;

      internal static bool IsPlainFileName(string fileName)
         => !string.IsNullOrWhiteSpace(fileName)
            && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && fileName is not ("." or "..");

      /// <summary>Core's <c>&lt;vault&gt;.&lt;32 hex&gt;.tmp</c> files for exactly this vault.</summary>
      internal static IEnumerable<string> ReplaceLeftovers(string vaultPath)
      {
         string? directory = Path.GetDirectoryName(vaultPath);
         if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
         {
            return [];
         }

         string prefix = Path.GetFileName(vaultPath) + ".";
         return Directory.EnumerateFiles(directory, "*.tmp")
            .Where(path =>
            {
               string name = Path.GetFileName(path);
               return name.StartsWith(prefix, StringComparison.Ordinal)
                  && _tmpSuffix().IsMatch(name[prefix.Length..]);
            })
            .ToArray();
      }

      [GeneratedRegex("^[0-9a-f]{32}\\.tmp$", RegexOptions.CultureInvariant)]
      private static partial Regex _tmpSuffix();
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

      public Task<IVaultSource?> CreateInteractiveAsync(string suggestedFileName, CancellationToken cancellationToken = default)
         => Task.FromResult<IVaultSource?>(null);
   }
}
