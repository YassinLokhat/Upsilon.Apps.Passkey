using Android.Content;
using Android.Provider;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using AndroidApp = Android.App.Application;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>
   /// SAF-backed vault: private working copy + SHA-256 fingerprint for conflict detection.
   /// SAF offers no atomic replace, so write-back keeps a pristine copy of the
   /// last known source, verifies the written bytes, and restores the pristine
   /// copy when the write did not land intact.
   /// <para>
   /// The private directory is derived from the source URI so an interrupted
   /// run is found again: <c>source.meta</c> binds it to its URI and pristine
   /// hash, and <c>checkout.complete</c> is written last so a partial copy is
   /// never mistaken for user edits.
   /// </para>
   /// </summary>
   internal sealed class SafVaultSource : IVaultSource
   {
      private const string META_FILE = "source.meta";
      private const string COMPLETE_MARKER = "checkout.complete";
      private const string CONFLICTS_DIR = "conflicts";
      private const int MAX_CONFLICT_COPIES = 5;

      private static readonly string _emptySha256 = Convert.ToHexString(SHA256.HashData([]));

      private readonly string _vaultsRoot;
      private readonly bool _isNew;
      private string _workDir;
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "The same Java URI instance is shared with AndroidVaultSourceFactory's registry for the process lifetime.")]
      private global::Android.Net.Uri _sourceUri;
      private string? _sourceSha256;

      /// <summary>
      /// True once the durable source matches the working copy; only then may
      /// <see cref="DisposeAsync"/> delete the private directory.
      /// </summary>
      private bool _safeToDelete;

      /// <summary>Write-back conflicts of this session share one copy, refreshed on each attempt.</summary>
      private string? _sessionConflictPath;

      private SafVaultSource(global::Android.Net.Uri sourceUri, string displayName, bool isNew)
      {
         _sourceUri = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
         DisplayName = displayName;
         _isNew = isNew;
         // NoBackupFilesDir: excluded from backup / device transfer (see ADR-001).
         string root = AndroidApp.Context.NoBackupFilesDir?.AbsolutePath
            ?? FileSystem.AppDataDirectory;
         _vaultsRoot = Path.Join(root, "vaults");
         _workDir = Path.Join(_vaultsRoot, _directoryKey(sourceUri));
      }

      public string WorkingPath => Path.Join(_workDir, "vault.pku");

      private string _pristinePath => Path.Join(_workDir, "pristine.pku");

      private string _snapshotPath => Path.Join(_workDir, "snapshot.pku");

      public string DisplayName { get; private set; }

      /// <summary>Full SAF URI: may reveal folder names, never log or display it.</summary>
      public string Location => _sourceUri.ToString()!;

      internal global::Android.Net.Uri SourceUri => _sourceUri;

      /// <summary>Existing document picked by the user.</summary>
      internal static SafVaultSource Create(global::Android.Net.Uri uri, string displayName)
         => new(uri, displayName, isNew: false);

      /// <summary>Empty document just created by the user; Core creates the vault in the working path.</summary>
      internal static SafVaultSource CreateNew(global::Android.Net.Uri uri, string displayName)
         => new(uri, displayName, isNew: true);

      public async Task<VaultCheckoutResult> CheckoutAsync(CancellationToken cancellationToken = default)
      {
         VaultCheckoutResult result = VaultCheckoutResult.Fresh;
         if (!_isNew)
         {
            (VaultCheckoutResult recovered, string? pristineHash) = await _recoverAsync(cancellationToken).ConfigureAwait(false);
            if (recovered == VaultCheckoutResult.RecoveredLocalChanges)
            {
               _sourceSha256 = pristineHash;
               _safeToDelete = false;
               return recovered;
            }

            result = recovered;
         }

         _resetWorkDir(keepConflicts: true);

         if (_isNew)
         {
            _sourceSha256 = _emptySha256;
            await File.WriteAllBytesAsync(_pristinePath, [], cancellationToken).ConfigureAwait(false);
         }
         else
         {
            Stream opened = _resolver().OpenInputStream(_sourceUri)
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
         }

         await _writeMetaAsync(cancellationToken).ConfigureAwait(false);
         await File.WriteAllBytesAsync(Path.Join(_workDir, COMPLETE_MARKER), [], cancellationToken).ConfigureAwait(false);
         _safeToDelete = true;
         return result;
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
            // Core may still hold the working copy open (flush while backgrounded)
            // and replaces it atomically, so copy a consistent snapshot first.
            _snapshot();
            string snapshotHash = await _hashFileAsync(_snapshotPath, cancellationToken).ConfigureAwait(false);
            if (string.Equals(snapshotHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
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
               _sessionConflictPath = _keepAsConflict(_snapshotPath, _sessionConflictPath);
               return VaultWriteBackResult.Conflict;
            }

            overwriting = true;
            if (!await _tryWriteSourceAsync(_snapshotPath, cancellationToken).ConfigureAwait(false)
                || !string.Equals(await _hashSourceAsync(cancellationToken).ConfigureAwait(false), snapshotHash, StringComparison.OrdinalIgnoreCase))
            {
               return await _restorePristineAsync().ConfigureAwait(false)
                  ? VaultWriteBackResult.Failed
                  : VaultWriteBackResult.SourceDamaged;
            }

            overwriting = false;
            _sourceSha256 = snapshotHash;
            File.Copy(_snapshotPath, _pristinePath, overwrite: true);
            await _writeMetaAsync(cancellationToken).ConfigureAwait(false);
            _safeToDelete = string.Equals(
               await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false),
               snapshotHash,
               StringComparison.OrdinalIgnoreCase);
            return VaultWriteBackResult.Written;
         }
         catch (Exception ex) when (ex is IOException or Java.Lang.Exception)
         {
            AppServices.Log.Error($"SAF write-back failed: {ErrorText.ForLog(ex)}");
            if (overwriting && !await _restorePristineAsync().ConfigureAwait(false))
            {
               return VaultWriteBackResult.SourceDamaged;
            }

            return VaultWriteBackResult.Failed;
         }
         finally
         {
            _tryDelete(_snapshotPath);
         }
      }

      public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
      {
         if (File.Exists(WorkingPath) || _sourceSha256 is null)
         {
            return false;
         }

         // A provider may hand the URI to another document after a sync
         // replace: only delete the exact bytes this session checked out.
         // An unreadable source (revoked grant, offline provider) may still
         // exist: refuse rather than report a delete that did not happen.
         string? remoteHash = await _hashSourceAsync(cancellationToken).ConfigureAwait(false);
         if (!string.Equals(remoteHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
         {
            _safeToDelete = false;
            return false;
         }

         try
         {
            if (!DocumentsContract.DeleteDocument(_resolver(), _sourceUri))
            {
               return false;
            }
         }
         catch (Java.Lang.Exception ex)
         {
            AppServices.Log.Error($"SAF delete failed: {ErrorText.ForLog(ex)}");
            return false;
         }

         AndroidVaultSourceFactory.Forget(_sourceUri);
         _resetWorkDir(keepConflicts: false);
         _safeToDelete = true;
         return true;
      }

      /// <summary>
      /// Only once everything reached the source (<see cref="_safeToDelete"/>)
      /// and the source still holds those bytes, so no pending write targets
      /// the old URI and no edit is stranded in the old private directory.
      /// </summary>
      public async Task<bool> RenameAsync(string newFileName, CancellationToken cancellationToken = default)
      {
         if (!PathVaultSource.IsPlainFileName(newFileName) || !_safeToDelete || _sourceSha256 is null)
         {
            return false;
         }

         try
         {
            string? remoteHash = await _hashSourceAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(remoteHash, _sourceSha256, StringComparison.OrdinalIgnoreCase))
            {
               return false;
            }

            global::Android.Net.Uri? renamed = DocumentsContract.RenameDocument(_resolver(), _sourceUri, newFileName);
            if (renamed is null)
            {
               return false;
            }

            // Grant on the new URI before releasing the old one, or the vault
            // becomes unreachable after a restart.
            AndroidVaultSourceFactory.TakePersistablePermission(renamed, write: true);
            // Providers may adjust the name ("name (1)"); keep what they report.
            string displayName = AndroidVaultSourceFactory.QueryDisplayName(renamed) ?? newFileName;
            AndroidVaultSourceFactory.Forget(_sourceUri);
            _sourceUri = renamed;
            DisplayName = displayName;
            AndroidVaultSourceFactory.RegisterSafUri(renamed, displayName);
            _moveWorkDirTo(Path.Join(_vaultsRoot, _directoryKey(renamed)));
            if (File.Exists(Path.Join(_workDir, META_FILE)))
            {
               await _writeMetaAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
         }
         catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException or IOException)
         {
            AppServices.Log.Warn($"SAF rename refused: {ErrorText.ForLog(ex)}");
            return false;
         }
      }

      private void _moveWorkDirTo(string target)
      {
         if (Directory.Exists(target))
         {
            // Leftovers keyed to the new URI belong to an older document.
            Directory.Delete(target, recursive: true);
         }

         if (Directory.Exists(_workDir))
         {
            Directory.Move(_workDir, target);
         }

         _workDir = target;
      }

      /// <summary>
      /// Deletes the private copies when the source is known to hold them.
      /// Otherwise (failed write, conflict) they are kept so no edit is lost.
      /// </summary>
      public ValueTask DisposeAsync()
      {
         if (_safeToDelete)
         {
            _resetWorkDir(keepConflicts: true);
            _tryDelete(Path.Join(_workDir, META_FILE));
            _tryDeleteDirectoryIfEmpty(_workDir);
         }

         return ValueTask.CompletedTask;
      }

      private async Task<(VaultCheckoutResult Result, string? PristineHash)> _recoverAsync(CancellationToken cancellationToken)
      {
         string metaPath = Path.Join(_workDir, META_FILE);
         if (!File.Exists(Path.Join(_workDir, COMPLETE_MARKER))
             || !File.Exists(metaPath)
             || !File.Exists(WorkingPath))
         {
            return (VaultCheckoutResult.Fresh, null);
         }

         string[] meta = await File.ReadAllLinesAsync(metaPath, cancellationToken).ConfigureAwait(false);
         if (meta.Length < 2 || !string.Equals(meta[0], _sourceUri.ToString(), StringComparison.Ordinal))
         {
            // Bound to another URI: never write it back here, never drop it.
            _keepAsConflict(WorkingPath);
            return (VaultCheckoutResult.StaleCopyKeptAsConflict, null);
         }

         string pristineHash = meta[1];
         string workingHash = await _hashFileAsync(WorkingPath, cancellationToken).ConfigureAwait(false);
         if (string.Equals(workingHash, pristineHash, StringComparison.OrdinalIgnoreCase))
         {
            return (VaultCheckoutResult.Fresh, null);
         }

         string? remoteHash = await _hashSourceAsync(cancellationToken).ConfigureAwait(false);
         if (string.Equals(remoteHash, pristineHash, StringComparison.OrdinalIgnoreCase))
         {
            return (VaultCheckoutResult.RecoveredLocalChanges, pristineHash);
         }

         _keepAsConflict(WorkingPath);
         return (VaultCheckoutResult.StaleCopyKeptAsConflict, null);
      }

      private void _snapshot()
      {
         using FileStream input = new(WorkingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         using FileStream output = new(_snapshotPath, FileMode.Create, FileAccess.Write, FileShare.None);
         input.CopyTo(output);
         output.Flush(flushToDisk: true);
      }

      /// <summary>
      /// Copies <paramref name="path"/> into the conflict folder, reusing <paramref name="target"/>
      /// when given, and keeps only the newest <see cref="MAX_CONFLICT_COPIES"/> copies.
      /// </summary>
      private string _keepAsConflict(string path, string? target = null)
      {
         string conflicts = Path.Join(_workDir, CONFLICTS_DIR);
         _ = Directory.CreateDirectory(conflicts);
         target ??= Path.Join(conflicts, $"{_safeFilePrefix(DisplayName)}-conflict-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}.pku");
         File.Copy(path, target, overwrite: true);

         foreach (FileInfo stale in new DirectoryInfo(conflicts).GetFiles("*.pku")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Skip(MAX_CONFLICT_COPIES))
         {
            _tryDelete(stale.FullName);
         }

         return target;
      }

      private static string _safeFilePrefix(string displayName)
      {
         char[] invalid = Path.GetInvalidFileNameChars();
         string safe = new([.. Path.GetFileNameWithoutExtension(displayName).Where(c => !invalid.Contains(c))]);
         return safe.Length == 0 ? "vault" : safe;
      }

      /// <summary>Conflict copies of every SAF vault, oldest first.</summary>
      internal static IEnumerable<FileInfo> EnumerateConflictCopies()
      {
         string root = Path.Join(AndroidApp.Context.NoBackupFilesDir?.AbsolutePath ?? FileSystem.AppDataDirectory, "vaults");
         return !Directory.Exists(root)
            ? []
            : new DirectoryInfo(root).EnumerateDirectories()
               .Select(d => new DirectoryInfo(Path.Join(d.FullName, CONFLICTS_DIR)))
               .Where(d => d.Exists)
               .SelectMany(d => d.EnumerateFiles("*.pku"))
               .OrderBy(f => f.LastWriteTimeUtc);
      }

      private Task _writeMetaAsync(CancellationToken cancellationToken)
         => File.WriteAllLinesAsync(Path.Join(_workDir, META_FILE), [_sourceUri.ToString()!, _sourceSha256!], cancellationToken);

      private void _resetWorkDir(bool keepConflicts)
      {
         if (!Directory.Exists(_workDir))
         {
            _ = Directory.CreateDirectory(_workDir);
            return;
         }

         // Working copy first: a crash mid-loop must not leave vault.pku
         // without its checkout marker and metadata.
         if (File.Exists(WorkingPath))
         {
            File.Delete(WorkingPath);
         }

         foreach (string file in Directory.EnumerateFiles(_workDir))
         {
            File.Delete(file);
         }

         string conflicts = Path.Join(_workDir, CONFLICTS_DIR);
         if (!keepConflicts && Directory.Exists(conflicts))
         {
            Directory.Delete(conflicts, recursive: true);
         }

         _ = Directory.CreateDirectory(_workDir);
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

      /// <returns><c>true</c> when the source again holds the pristine bytes.</returns>
      private async Task<bool> _restorePristineAsync()
      {
         try
         {
            if (File.Exists(_pristinePath)
                && await _tryWriteSourceAsync(_pristinePath, CancellationToken.None).ConfigureAwait(false))
            {
               return string.Equals(
                  await _hashSourceAsync(CancellationToken.None).ConfigureAwait(false),
                  _sourceSha256,
                  StringComparison.OrdinalIgnoreCase);
            }
         }
         catch (Exception ex) when (ex is IOException or Java.Lang.Exception)
         {
            AppServices.Log.Error($"SAF restore of the original vault failed: {ErrorText.ForLog(ex)}");
         }

         return false;
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

      private static string _directoryKey(global::Android.Net.Uri uri)
         => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.ToString()!)))[..32];

      private static void _tryDelete(string path)
      {
         try
         {
            File.Delete(path);
         }
         catch (IOException ex)
         {
            AppServices.Log.Warn($"Vault temporary file cleanup failed: {ErrorText.ForLog(ex)}");
         }
      }

      private static void _tryDeleteDirectoryIfEmpty(string directory)
      {
         if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
         {
            Directory.Delete(directory);
         }
      }

      private static ContentResolver _resolver()
         => AndroidApp.Context.ContentResolver
            ?? throw new InvalidOperationException("ContentResolver unavailable.");

      private static async Task<string> _hashFileAsync(string path, CancellationToken cancellationToken)
      {
         FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         await using (stream.ConfigureAwait(false))
         {
            return await _hashStreamAsync(stream, cancellationToken).ConfigureAwait(false);
         }
      }

      private static async Task<string> _hashStreamAsync(Stream stream, CancellationToken cancellationToken)
      {
         byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
         return Convert.ToHexString(hash);
      }
   }
}
