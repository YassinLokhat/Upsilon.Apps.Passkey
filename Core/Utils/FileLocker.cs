using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal class FileLocker : IDisposable
   {
      public string FilePath { get; private set; }
      private FileStream? _stream;
      private FileMode _fileMode;

      public FileLocker(string filePath, FileMode fileMode = FileMode.Open)
      {
         FilePath = filePath;
         _fileMode = fileMode;

         Lock();
      }

      public void Lock()
      {
         Unlock();

         _stream = new FileStream(FilePath, _fileMode, FileAccess.ReadWrite, FileShare.None);
      }

      public void Unlock()
      {
         if (_stream == null) return;

         _stream.Close();
         _stream.Dispose();
         _stream = null;
      }

      public string ReadAllText()
      {
         Unlock();

         string text = File.ReadAllText(FilePath);

         Lock();

         return text;
      }

      public string ReadAllText(string[] passkeys)
      {
         string text = ReadAllText();

         return SecurityCenter.Decrypt(text, passkeys);
      }

      public void WriteAllText(string text)
      {
         Unlock();

         File.WriteAllText(FilePath, text);

         Lock();
      }

      public void WriteAllText(string text, string[] passkeys)
      {
         text = SecurityCenter.Encrypt(text, passkeys);

         WriteAllText(text);
      }

      public void Delete()
      {
         Unlock();
         File.Delete(FilePath);
      }

      public void Dispose()
      {
         Unlock();
         FilePath = string.Empty;
      }
   }
}
