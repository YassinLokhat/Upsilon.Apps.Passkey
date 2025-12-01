using System.IO.Compression;
using System.Text;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
using Upsilon.Apps.Passkey.Core.Public.Utils;

namespace Upsilon.Apps.Passkey.Core.Internal.Utils
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
         return ReadAllText(passkeys).DeserializeTo<T>(_serializationCenter);
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
         WriteAllText(obj.SerializeWith(_serializationCenter), passkeys);
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

   }
}
