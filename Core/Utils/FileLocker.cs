using System.IO.Compression;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
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
      // never an unlocked window between operations during which another process
      // could grab a write handle. FileShare.Read still lets other processes
      // open the file for reading (backups, antivirus, inspection) while denying
      // concurrent writers — a second FileLocker on the same path still fails.
      private const FileShare ShareMode = FileShare.Read;

      internal FileLocker(ICryptographyCenter cryptographicCenter, ISerializationCenter serializationCenter, string filePath, FileMode fileMode = FileMode.Open)
      {
         FilePath = filePath;

         _cryptographicCenter = cryptographicCenter;
         _serializationCenter = serializationCenter;

         _stream = new FileStream(FilePath, fileMode, FileAccess.ReadWrite, ShareMode);
      }

      private FileStream Stream => _stream
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
            using ZipArchive archive = _openArchive(ZipArchiveMode.Update);
            archive.GetEntry(fileEntry)?.Delete();
         }
      }

      internal bool Exists(string fileEntry)
      {
         lock (_gate)
         {
            // An empty file (just created, not yet written) is not a valid zip
            // yet, so it cannot contain any entry.
            if (Stream.Length == 0)
            {
               return false;
            }

            using ZipArchive archive = _openArchive(ZipArchiveMode.Read);
            return archive.GetEntry(fileEntry) is not null;
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
         if (_stream is null) return;

         _stream.Dispose();
         _stream = null;
      }

      private ZipArchive _openArchive(ZipArchiveMode mode)
      {
         Stream.Position = 0;
         return new ZipArchive(Stream, mode, leaveOpen: true, Encoding.UTF8);
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
            byte[] bytes = Convert.FromBase64String(compressedText);
            using MemoryStream msi = new(bytes);
            using MemoryStream mso = new();
            using (GZipStream gs = new(msi, CompressionMode.Decompress))
            {
               gs.CopyTo(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
         }
         catch
         {
            throw new CorruptedSourceException();
         }
      }

      private string _readContent(string fileEntry, string[] passkeys)
      {
         using ZipArchive archive = _openArchive(ZipArchiveMode.Read);

         ZipArchiveEntry zipEntry = archive.GetEntry(fileEntry)
            ?? throw new FileNotFoundException($"The file entry '{fileEntry}' not found in the archive {FilePath}.", $"{FilePath}/{fileEntry}");

         using Stream stream = zipEntry.Open();
         using StreamReader reader = new(stream, Encoding.UTF8);

         string content = reader.ReadToEnd();

         return passkeys.Length != 0
            ? _cryptographicCenter.DecryptSymmetrically(_decompressString(content), passkeys)
            : _decompressString(content);
      }

      private void _writeContent(string content, string fileEntry, string[] passkeys)
      {
         using (ZipArchive archive = _openArchive(ZipArchiveMode.Update))
         {
            archive.GetEntry(fileEntry)?.Delete();

            ZipArchiveEntry newEntry = archive.CreateEntry(fileEntry);

            using Stream stream = newEntry.Open();
            using StreamWriter writer = new(stream, Encoding.UTF8);

            if (passkeys.Length != 0)
            {
               writer.Write(_compressString(_cryptographicCenter.EncryptSymmetrically(content, passkeys)));
            }
            else
            {
               writer.Write(_compressString(content));
            }
         }

         // ZipArchive.Update rewrites the archive on dispose; rewind so the next
         // open starts from a known position.
         Stream.Position = 0;
      }
   }
}
