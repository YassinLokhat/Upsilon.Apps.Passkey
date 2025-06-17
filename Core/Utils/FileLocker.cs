using Upsilon.Apps.PassKey.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Utils
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

         string text = File.ReadAllText(FilePath);

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

         File.WriteAllText(FilePath, text);

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
   }
}
