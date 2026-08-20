using System.IO.Compression;
using System.Security;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Exclusive owner of a <c>.pku</c> ZIP: session-long handle, atomic replace
   /// on write, JSON → GZip → (optional onion) per entry. All public methods
   /// take the re-entrant <c>_gate</c>.
   /// </summary>
   internal sealed class FileLocker : IDisposable
   {
      internal string FilePath { get; private set; }
      private FileStream? _stream;
      private readonly ICryptographyCenter _cryptographicCenter;
      private readonly ISerializationCenter _serializationCenter;

      // Serializes every public operation so two threads (e.g. a save and the
      // session-timeout timer) cannot touch the archive at the same time. The
      // lock is re-entrant, which matches how nested call paths reach the file.
      private readonly System.Threading.Lock _gate = new();

      // The .pku is held open for the whole lifetime of this locker: there is
      // never an unlocked window between *logical* operations during which
      // another process could grab a write handle — except for the brief
      // close/reopen inside _commitArchiveAtomically, which swaps a fully
      // written sibling temp file into place. FileShare.Read lets other
      // processes open the file for reading (backups, antivirus, inspection)
      // while denying concurrent writers. FileShare.Delete lets the atomic
      // replace succeed even when a reader still holds a handle that allowed
      // deletion (common on Windows with scanners).
      private const FileShare SHARE_MODE = FileShare.Read | FileShare.Delete;

      // Windows antivirus / search indexers often open a just-closed .pku for
      // a few milliseconds; File.Move(overwrite) then fails with
      // UnauthorizedAccessException or IOException (HRESULT 0x80070005). Retry
      // with short backoff — the same strategy used by the .NET SDK tooling.
      private const int REPLACE_MAX_ATTEMPTS = 16;
      private static readonly TimeSpan _replaceInitialDelay = TimeSpan.FromMilliseconds(5);

      internal FileLocker(ICryptographyCenter cryptographicCenter, ISerializationCenter serializationCenter, string filePath, FileMode fileMode = FileMode.Open)
      {
         FilePath = filePath;

         _cryptographicCenter = cryptographicCenter;
         _serializationCenter = serializationCenter;

         _stream = new FileStream(FilePath, fileMode, FileAccess.ReadWrite, SHARE_MODE);
      }

      private FileStream _stream2 => _stream
         ?? throw new ObjectDisposedException(nameof(FileLocker));

      internal T Open<T>(string fileEntry, string[] passkeys) where T : notnull
      {
         lock (_gate)
         {
            return _readContent(fileEntry, passkeys).DeserializeTo<T>(_serializationCenter);
         }
      }

      internal T Open<T>(string fileEntry) where T : notnull => Open<T>(fileEntry, []);

      internal void Save<T>(T obj, string fileEntry, string[] passkeys) where T : notnull
      {
         lock (_gate)
         {
            _writeContent(obj.SerializeWith(_serializationCenter), fileEntry, passkeys);
         }
      }

      internal void Save<T>(T obj, string fileEntry) where T : notnull => Save(obj, fileEntry, []);

      internal void Delete()
      {
         lock (_gate)
         {
            _releaseStream();

            if (File.Exists(FilePath))
            {
               File.Delete(FilePath);
            }
         }
      }

      internal void Delete(string fileEntry)
      {
         lock (_gate)
         {
            if (_stream2.Length == 0
               || !_entryExists(fileEntry))
            {
               return;
            }

            byte[] updated = _buildArchive(fileEntry, payload: null);
            _commitArchiveAtomically(updated);
         }
      }

      internal bool Exists(string fileEntry)
      {
         lock (_gate)
         {
            // An empty file (just created, not yet written) is not a valid zip
            // yet, so it cannot contain any entry.
            return _stream2.Length != 0 && _entryExists(fileEntry);
         }
      }

      public void Dispose()
      {
         lock (_gate)
         {
            _releaseStream();
            FilePath = string.Empty;
         }
      }

      private void _releaseStream()
      {
         if (_stream is null)
         {
            return;
         }

         _stream.Dispose();
         _stream = null;
      }

      private ZipArchive _openArchive(ZipArchiveMode mode)
      {
         _stream2.Position = 0;
         return new ZipArchive(_stream2, mode, leaveOpen: true, Encoding.UTF8);
      }

      private bool _entryExists(string fileEntry)
      {
         using ZipArchive archive = _openArchive(ZipArchiveMode.Read);
         return archive.GetEntry(fileEntry) is not null;
      }

      private static string _compressString(string text)
      {
         byte[] bytes = Encoding.UTF8.GetBytes(text);
         using MemoryStream msi = new(bytes);
         using MemoryStream mso = new();
         using (GZipStream gs = new(mso, CompressionLevel.SmallestSize))
         {
            msi.CopyTo(gs);
         }
         return Convert.ToBase64String(mso.ToArray());
      }

      private static string _decompressString(string compressedText)
      {
         try
         {
            return _decompressStringCore(compressedText);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            || ex is ArgumentNullException
            || ex is FormatException
            || ex is NotSupportedException
            || ex is FormatException
            || ex is ObjectDisposedException
            || ex is IOException
            || ex is DecoderFallbackException)
         {
            throw new CorruptedSourceException("Compressed payload could not be decoded.", ex);
         }
      }

      // Shared GZip/Base64 decode without mapping failures to a domain exception;
      // keyed reads need to distinguish "onion not finished yet" from corruption.
      private static string _decompressStringCore(string compressedText)
      {
         byte[] bytes = Convert.FromBase64String(compressedText);
         using MemoryStream msi = new(bytes);
         using MemoryStream mso = new();
         using (GZipStream gs = new(msi, CompressionMode.Decompress))
         {
            gs.CopyTo(mso);
         }
         return Encoding.UTF8.GetString(mso.ToArray());
      }

      private string _readContent(string fileEntry, string[] passkeys)
      {
         using ZipArchive archive = _openArchive(ZipArchiveMode.Read);

         ZipArchiveEntry zipEntry = archive.GetEntry(fileEntry)
            ?? throw new FileNotFoundException($"The file entry '{fileEntry}' not found in the archive {FilePath}.", $"{FilePath}/{fileEntry}");

         using Stream stream = zipEntry.Open();
         using StreamReader reader = new(stream, Encoding.UTF8);

         string content = reader.ReadToEnd();

         // Inverse of write: decrypt (when keyed) then decompress. Compression
         // runs on plaintext so GZip actually shrinks JSON; ciphertext would not.
         if (passkeys.Length == 0)
         {
            return _decompressString(content);
         }

         string decrypted = _cryptographicCenter.DecryptSymmetrically(content, passkeys);

         try
         {
            return _decompressStringCore(decrypted);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            || ex is ArgumentNullException
            || ex is FormatException
            || ex is NotSupportedException
            || ex is FormatException
            || ex is ObjectDisposedException
            || ex is IOException
            || ex is DecoderFallbackException)
         {
            // Layers peeled cleanly but the payload is not valid gzip yet: either
            // more passkeys are required (progressive login) or the inner bytes
            // are junk. Login treats this as a soft miss; outer AEAD failures
            // already surfaced as CorruptedSourceException / WrongPasswordException.
            throw new IncompleteOnionException("Decrypted payload is not a finished vault entry yet.", ex);
         }
      }

      private void _writeContent(string content, string fileEntry, string[] passkeys)
      {
         // Compress-then-encrypt: GZip the JSON first, then (optionally) wrap
         // the compressed payload in the symmetric onion. Encrypting first
         // would leave GZip with high-entropy input and almost no size gain.
         string compressed = _compressString(content);
         string payload = passkeys.Length != 0
            ? _cryptographicCenter.EncryptSymmetrically(compressed, passkeys)
            : compressed;

         // Build the full updated archive off to the side, then swap it in
         // atomically. In-place ZipArchiveMode.Update on a live FileStream can
         // leave trailing garbage when the archive shrinks, and a crash mid-
         // rewrite can leave a half-updated .pku with no intact predecessor.
         byte[] updated = _buildArchive(fileEntry, payload);
         _commitArchiveAtomically(updated);
      }

      // When payload is non-null, add or replace fileEntry. When null, omit it
      // (delete). Other existing entries are copied through unchanged.
      private byte[] _buildArchive(string fileEntry, string? payload)
      {
         using MemoryStream output = new();

         using (ZipArchive outArchive = new(output, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
         {
            bool wroteTarget = false;

            if (_stream2.Length > 0)
            {
               _stream2.Position = 0;
               using ZipArchive inArchive = new(_stream2, ZipArchiveMode.Read, leaveOpen: true, Encoding.UTF8);

               foreach (ZipArchiveEntry entry in inArchive.Entries)
               {
                  if (string.Equals(entry.FullName, fileEntry, StringComparison.Ordinal))
                  {
                     if (payload is not null)
                     {
                        _writeZipEntry(outArchive, fileEntry, payload);
                        wroteTarget = true;
                     }

                     continue;
                  }

                  ZipArchiveEntry copy = outArchive.CreateEntry(entry.FullName);
                  using Stream source = entry.Open();
                  using Stream destination = copy.Open();
                  source.CopyTo(destination);
               }
            }

            if (payload is not null
               && !wroteTarget)
            {
               _writeZipEntry(outArchive, fileEntry, payload);
            }
         }

         return output.ToArray();
      }

      private static void _writeZipEntry(ZipArchive archive, string fileEntry, string payload)
      {
         ZipArchiveEntry entry = archive.CreateEntry(fileEntry);
         using Stream stream = entry.Open();
         using StreamWriter writer = new(stream, Encoding.UTF8);
         writer.Write(payload);
      }

      // Durability strategy: write the complete archive to a sibling temp file
      // (flushed to disk), release our handle, then File.Move(overwrite) so
      // readers either see the previous intact .pku or the new intact one —
      // never a torn ZipArchive.Update. The handle is reacquired immediately
      // afterwards; the unlocked window is only the replace itself. On Windows
      // that window races with AV/indexers, so the move is retried; if it still
      // cannot replace, we fall back to an in-place rewrite under a reacquired
      // handle (still truncates correctly via SetLength).
      private void _commitArchiveAtomically(ReadOnlySpan<byte> archiveBytes)
      {
         string directory = Path.GetDirectoryName(FilePath) is { Length: > 0 } dir
            ? dir
            : ".";

         string tempPath = Path.Combine(
            directory,
            $"{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

         byte[] payload = archiveBytes.ToArray();

         try
         {
            using (FileStream temp = new(
               tempPath,
               FileMode.CreateNew,
               FileAccess.Write,
               FileShare.None,
               bufferSize: 64 * 1024,
               FileOptions.WriteThrough))
            {
               temp.Write(payload);
               temp.Flush(flushToDisk: true);
            }

            string path = FilePath;
            _releaseStream();

            bool replaced = _tryReplaceWithRetries(tempPath, path);

            if (replaced)
            {
               tempPath = string.Empty;
               _stream = _openExistingWithRetries(path);
            }
            else
            {
               // Move kept failing (typically a transient scanner lock). Prefer
               // a successful in-place commit over failing the whole Save: the
               // temp file still holds a complete archive if we crash mid-write.
               System.Diagnostics.Trace.TraceWarning(
                  $"Atomic replace of '{path}' failed after retries; falling back to in-place rewrite.");

               _stream = _openExistingWithRetries(path);
               _rewriteInPlace(payload);
            }
         }
         finally
         {
            if (tempPath.Length != 0
               && File.Exists(tempPath))
            {
               try
               {
                  File.Delete(tempPath);
               }
               catch (Exception ex)
                  when (ex is ArgumentException
                  || ex is ArgumentNullException
                  || ex is PathTooLongException
                  || ex is DirectoryNotFoundException
                  || ex is IOException
                  || ex is UnauthorizedAccessException
                  || ex is NotSupportedException
                  || ex is SecurityException)
               {
                  System.Diagnostics.Trace.TraceWarning($"Failed to delete file '{tempPath}'");
               }
            }
         }
      }

      private void _rewriteInPlace(ReadOnlySpan<byte> archiveBytes)
      {
         FileStream stream = _stream2;
         stream.Position = 0;
         stream.Write(archiveBytes);
         stream.SetLength(archiveBytes.Length);
         stream.Flush(flushToDisk: true);
      }

      private static bool _tryReplaceWithRetries(string tempPath, string path)
      {
         TimeSpan delay = _replaceInitialDelay;
         Exception? lastFailure = null;

         for (int attempt = 1; attempt <= REPLACE_MAX_ATTEMPTS; attempt++)
         {
            try
            {
               File.Move(tempPath, path, overwrite: true);
               return true;
            }
            catch (Exception ex) when (_isTransientFileAccessFailure(ex))
            {
               lastFailure = ex;

               if (attempt == REPLACE_MAX_ATTEMPTS)
               {
                  break;
               }

               Thread.Sleep(delay);
               delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 200));
            }
         }

         System.Diagnostics.Trace.TraceWarning(
            $"File.Move('{tempPath}' → '{path}') failed after {REPLACE_MAX_ATTEMPTS} attempts: {lastFailure}");
         return false;
      }

      private static FileStream _openExistingWithRetries(string path)
      {
         TimeSpan delay = _replaceInitialDelay;
         Exception? lastFailure = null;

         for (int attempt = 1; attempt <= REPLACE_MAX_ATTEMPTS; attempt++)
         {
            try
            {
               return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, SHARE_MODE);
            }
            catch (Exception ex) when (attempt < REPLACE_MAX_ATTEMPTS && _isTransientFileAccessFailure(ex))
            {
               lastFailure = ex;
               Thread.Sleep(delay);
               delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 200));
            }
         }

         throw lastFailure
            ?? new IOException($"Could not reopen '{path}' after atomic replace.");
      }

      private static bool _isTransientFileAccessFailure(Exception ex) =>
         ex is UnauthorizedAccessException
            or IOException;
   }
}
