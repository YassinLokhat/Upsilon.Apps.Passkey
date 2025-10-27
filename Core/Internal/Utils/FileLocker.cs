using System.IO.Compression;
using System.Text;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.PassKey.Core.Internal.Utils
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

         _stream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
      }

      internal void Unlock()
      {
         if (_stream == null) return;

         _stream.Close();
         _stream.Dispose();
         _stream = null;
      }

      internal string ReadAllText()
      {
         Unlock();

         string text = _decompressString(File.ReadAllText(FilePath));

         Lock();

         return text;
      }

      internal string ReadAllText(string[] passkeys)
      {
         string text = ReadAllText();

         return _cryptographicCenter.DecryptSymmetrically(text, passkeys);
      }

      internal T Open<T>(string[] passkeys) where T : notnull
      {
         return _serializationCenter.Deserialize<T>(ReadAllText(passkeys));
      }

      internal void WriteAllText(string text)
      {
         Unlock();

         File.WriteAllText(FilePath, _compressString(text));

         Lock();
      }

      internal void WriteAllText(string text, string[] passkeys)
      {
         text = _cryptographicCenter.EncryptSymmetrically(text, passkeys);

         WriteAllText(text);
      }

      internal void Save<T>(T obj, string[] passkeys) where T : notnull
      {
         WriteAllText(_serializationCenter.Serialize(obj), passkeys);
      }

      internal void Delete()
      {
         Unlock();

         if (File.Exists(FilePath))
         {
            File.Delete(FilePath);
         }
      }

      public void Dispose()
      {
         Unlock();
         FilePath = string.Empty;
      }

      private static string _compressString(string text)
      {
         var bytes = Encoding.UTF8.GetBytes(text);
         using var msi = new MemoryStream(bytes);
         using var mso = new MemoryStream();
         using (var gs = new GZipStream(mso, CompressionLevel.SmallestSize))
         {
            msi.CopyTo(gs);
         }
         return Convert.ToBase64String(mso.ToArray());
      }

      private static string _decompressString(string compressedText)
      {
         try
         {
            var bytes = Convert.FromBase64String(compressedText);
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
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

   }
}
