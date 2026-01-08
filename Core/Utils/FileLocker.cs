using System.IO.Compression;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal class FileLocker : IDisposable
   {
      internal string FilePath { get; private set; }
      private FileStream? _stream;
      private readonly ICryptographyCenter _cryptographicCenter;
      private readonly ISerializationCenter _serializationCenter;

      internal FileLocker(ICryptographyCenter cryptographicCenter, ISerializationCenter serializationCenter, string filePath, FileMode fileMode = FileMode.Open)
      {
         FilePath = filePath;

         _cryptographicCenter = cryptographicCenter;
         _serializationCenter = serializationCenter;

         _stream = new FileStream(FilePath, fileMode, FileAccess.ReadWrite, FileShare.None);
      }

      internal void Lock()
      {
         Unlock();

         _stream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
      }

      internal void Unlock()
      {
         if (_stream is null) return;

         _stream.Close();
         _stream.Dispose();
         _stream = null;
      }

      internal T Open<T>(string fileEntry, string[] passkeys) where T : notnull
      {
         return _readContent(fileEntry, passkeys).DeserializeTo<T>(_serializationCenter);
      }

      internal T Open<T>(string fileEntry) where T : notnull => Open<T>(fileEntry, []);

      internal void Save<T>(T obj, string fileEntry, string[] passkeys) where T : notnull
      {
         _writeContent(obj.SerializeWith(_serializationCenter), fileEntry, passkeys);
      }

      internal void Save<T>(T obj, string fileEntry) where T : notnull => Save(obj, fileEntry, []);

      internal void Delete()
      {
         Unlock();

         if (File.Exists(FilePath))
         {
            File.Delete(FilePath);
         }
      }

      internal void Delete(string fileEntry)
      {
         Unlock();

         using (ZipArchive archive = ZipFile.Open(FilePath, ZipArchiveMode.Update, Encoding.UTF8))
         {
            ZipArchiveEntry? existingEntry = archive.GetEntry(fileEntry);
            existingEntry?.Delete();
         }

         Lock();
      }

      internal bool Exists(string fileEntry)
      {
         Unlock();

         bool exists = false;

         using (ZipArchive archive = ZipFile.Open(FilePath, ZipArchiveMode.Update, Encoding.UTF8))
         {
            exists = archive.GetEntry(fileEntry) is not null;
         }

         Lock();

         return exists;
      }

      public void Dispose()
      {
         Unlock();
         FilePath = string.Empty;
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
         Unlock();
         string content;

         using (ZipArchive archive = ZipFile.OpenRead(FilePath))
         {
            ZipArchiveEntry zipEntry = archive.GetEntry(fileEntry)
               ?? throw new FileNotFoundException($"The file entry '{fileEntry}' not found in the archive {FilePath}.", $"{FilePath}/{fileEntry}");

            using Stream stream = zipEntry.Open();
            using StreamReader reader = new(stream, Encoding.UTF8);

            content = passkeys.Length != 0
               ? _cryptographicCenter.DecryptSymmetrically(_decompressString(reader.ReadToEnd()), passkeys)
               : _decompressString(reader.ReadToEnd());
         }

         Lock();

         return content;
      }

      private void _writeContent(string content, string fileEntry, string[] passkeys)
      {
         Unlock();

         using (ZipArchive archive = ZipFile.Open(FilePath, ZipArchiveMode.Update, Encoding.UTF8))
         {
            ZipArchiveEntry? existingEntry = archive.GetEntry(fileEntry);
            existingEntry?.Delete();

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

         Lock();
      }
   }
}
