using Android.Content;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// SAF-backed vault: private working copy + SHA-256 fingerprint for conflict detection.
   /// </summary>
   internal sealed class SafVaultSource : IVaultSource
   {
      private readonly global::Android.Net.Uri _sourceUri;
      private readonly string _workDir;
      private string? _sourceSha256;

      public SafVaultSource(global::Android.Net.Uri sourceUri, string displayName)
      {
         _sourceUri = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
         DisplayName = displayName;
         // NoBackupFilesDir: excluded from backup / device transfer (see ADR-001).
         string root = AndroidApp.Context.NoBackupFilesDir?.AbsolutePath
            ?? FileSystem.AppDataDirectory;
         _workDir = Path.Join(root, "vaults", Guid.NewGuid().ToString("N"));
         _ = Directory.CreateDirectory(_workDir);
         WorkingPath = Path.Join(_workDir, "vault.pku");
      }

      public string WorkingPath { get; }

      public string DisplayName { get; }

      /// <summary>Factory used by the Android host (also satisfies CA1812).</summary>
      internal static SafVaultSource Create(global::Android.Net.Uri uri, string displayName)
         => new(uri, displayName);

      public async Task CheckoutAsync(CancellationToken cancellationToken = default)
      {
         ContentResolver resolver = AndroidApp.Context.ContentResolver
            ?? throw new InvalidOperationException("ContentResolver unavailable.");

         Stream? opened = resolver.OpenInputStream(_sourceUri);
         if (opened is null)
         {
            throw new IOException("Cannot open SAF URI for read.");
         }

         await using (opened.ConfigureAwait(false))
         {
            FileStream output = new(WorkingPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (output.ConfigureAwait(false))
            {
               await opened.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
               await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
         }

         _sourceSha256 = await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false);
      }

      public async Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default)
      {
         if (_sourceSha256 is null
             || !File.Exists(WorkingPath))
         {
            return VaultWriteBackResult.Failed;
         }

         ContentResolver resolver = AndroidApp.Context.ContentResolver
            ?? throw new InvalidOperationException("ContentResolver unavailable.");

         Stream? remote = resolver.OpenInputStream(_sourceUri);
         if (remote is null)
         {
            return VaultWriteBackResult.Failed;
         }

         string remoteHash;
         await using (remote.ConfigureAwait(false))
         {
            remoteHash = await _hashStreamAsync(remote, cancellationToken).ConfigureAwait(false);
         }

         if (!string.Equals(remoteHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
         {
            string conflictPath = Path.Join(_workDir, $"conflict-{DateTime.UtcNow:yyyyMMddHHmmss}.pku");
            File.Copy(WorkingPath, conflictPath, overwrite: true);
            return VaultWriteBackResult.Conflict;
         }

         Stream? output = resolver.OpenOutputStream(_sourceUri, "wt");
         if (output is null)
         {
            return VaultWriteBackResult.Failed;
         }

         await using (output.ConfigureAwait(false))
         {
            FileStream input = new(WorkingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (input.ConfigureAwait(false))
            {
               await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
               await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
         }

         _sourceSha256 = await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false);
         return VaultWriteBackResult.Written;
      }

      private static async Task<string> _hashFileAsync(string path, CancellationToken cancellationToken)
      {
         FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
         await using (stream.ConfigureAwait(false))
         {
            return await _hashStreamAsync(stream, cancellationToken).ConfigureAwait(false);
         }
      }

      private static async Task<string> _hashStreamAsync(Stream stream, CancellationToken cancellationToken)
      {
         using var sha = System.Security.Cryptography.SHA256.Create();
         byte[] hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
         return Convert.ToHexString(hash);
      }
   }
}
