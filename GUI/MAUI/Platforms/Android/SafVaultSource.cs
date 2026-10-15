using Android.Content;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// SAF-backed vault: private working copy + SHA-256 fingerprint for conflict detection.
   /// SAF offers no atomic replace, so write-back keeps a pristine copy of the
   /// checked-out source, verifies the written bytes, and restores the pristine
   /// copy when the write did not land intact.
   /// </summary>
   internal sealed class SafVaultSource : IVaultSource
   {
      private readonly global::Android.Net.Uri _sourceUri;
      private readonly string _workDir;
      private readonly string _pristinePath;
      private string? _sourceSha256;

      /// <summary>
      /// True once the durable source matches the working copy; only then may
      /// <see cref="DisposeAsync"/> delete the private directory.
      /// </summary>
      private bool _safeToDelete;

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
         _pristinePath = Path.Join(_workDir, "pristine.pku");
      }

      public string WorkingPath { get; }

      public string DisplayName { get; }

      /// <summary>Factory used by the Android host (also satisfies CA1812).</summary>
      internal static SafVaultSource Create(global::Android.Net.Uri uri, string displayName)
         => new(uri, displayName);

      public async Task CheckoutAsync(CancellationToken cancellationToken = default)
      {
         ContentResolver resolver = _resolver();

         Stream opened = resolver.OpenInputStream(_sourceUri)
            ?? throw new IOException("Cannot open the vault source for reading.");

         await using (opened.ConfigureAwait(false))
         {
            FileStream output = new(WorkingPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (output.ConfigureAwait(false))
            {
               await opened.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
               await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
         }

         File.Copy(WorkingPath, _pristinePath, overwrite: true);
         _sourceSha256 = await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false);
         _safeToDelete = true;
      }

      public async Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default)
      {
         if (_sourceSha256 is null
             || !File.Exists(WorkingPath))
         {
            return VaultWriteBackResult.Failed;
         }

         bool overwriting = false;
         try
         {
            string workingHash = await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false);
            if (string.Equals(workingHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
            {
               _safeToDelete = true;
               return VaultWriteBackResult.Skipped;
            }

            _safeToDelete = false;

            // Checked immediately before opening for write. A change landing
            // between this read and the write below cannot be detected through
            // SAF; that residual window is accepted.
            string? remoteHash = await _hashSourceAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHash is null)
            {
               return VaultWriteBackResult.Failed;
            }

            if (!string.Equals(remoteHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
            {
               string conflictPath = Path.Join(_workDir, $"conflict-{DateTime.UtcNow:yyyyMMddHHmmss}.pku");
               File.Copy(WorkingPath, conflictPath, overwrite: true);
               return VaultWriteBackResult.Conflict;
            }

            overwriting = true;
            if (!await _tryWriteSourceAsync(WorkingPath, cancellationToken).ConfigureAwait(false)
                || !string.Equals(await _hashSourceAsync(cancellationToken).ConfigureAwait(false), workingHash, StringComparison.OrdinalIgnoreCase))
            {
               await _restorePristineAsync().ConfigureAwait(false);
               return VaultWriteBackResult.Failed;
            }

            overwriting = false;
            _sourceSha256 = workingHash;
            File.Copy(WorkingPath, _pristinePath, overwrite: true);
            _safeToDelete = true;
            return VaultWriteBackResult.Written;
         }
         catch (Exception ex) when (ex is IOException or Java.Lang.Exception)
         {
            AppServices.Log.Error($"SAF write-back failed: {ErrorText.ForLog(ex)}");
            if (overwriting)
            {
               await _restorePristineAsync().ConfigureAwait(false);
            }

            return VaultWriteBackResult.Failed;
         }
      }

      /// <summary>
      /// Deletes the private copies when the source is known to hold them.
      /// Otherwise (failed write, conflict) they are kept so no edit is lost.
      /// </summary>
      public ValueTask DisposeAsync()
      {
         if (_safeToDelete && Directory.Exists(_workDir))
         {
            Directory.Delete(_workDir, recursive: true);
         }

         return ValueTask.CompletedTask;
      }

      private async Task<bool> _tryWriteSourceAsync(string path, CancellationToken cancellationToken)
      {
         Stream? output = _resolver().OpenOutputStream(_sourceUri, "wt");
         if (output is null)
         {
            return false;
         }

         await using (output.ConfigureAwait(false))
         {
            FileStream input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (input.ConfigureAwait(false))
            {
               await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
               await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
         }

         return true;
      }

      private async Task _restorePristineAsync()
      {
         try
         {
            if (File.Exists(_pristinePath))
            {
               _ = await _tryWriteSourceAsync(_pristinePath, CancellationToken.None).ConfigureAwait(false);
            }
         }
         catch (Exception ex) when (ex is IOException or Java.Lang.Exception)
         {
            AppServices.Log.Error($"SAF restore of the original vault failed: {ErrorText.ForLog(ex)}");
         }
      }

      private async Task<string?> _hashSourceAsync(CancellationToken cancellationToken)
      {
         Stream? remote = _resolver().OpenInputStream(_sourceUri);
         if (remote is null)
         {
            return null;
         }

         await using (remote.ConfigureAwait(false))
         {
            return await _hashStreamAsync(remote, cancellationToken).ConfigureAwait(false);
         }
      }

      private static ContentResolver _resolver()
         => AndroidApp.Context.ContentResolver
            ?? throw new InvalidOperationException("ContentResolver unavailable.");

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
