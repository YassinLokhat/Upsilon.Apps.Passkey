using System.Security.Cryptography;
using System.Text;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   public static class SecurityCenter
   {
      public static string GetHash(this string source)
      {
         MD5 md5 = MD5.Create();

         var hash = md5
            .ComputeHash(Encoding.UTF8.GetBytes(source))
            .Select(x => x.ToString("X2"));

         return string.Join(string.Empty, hash);
      }

      public static string GetSlowHash(this string source, byte timeFactor)
      {
         long realTimeFactor = (long)Math.Pow(0b1000, timeFactor);

         for (int i = 0; i < realTimeFactor; i++)
         {
            source = source.GetHash();
         }

         return source;
      }

      public static int HashLength => GetHash(string.Empty).Length;

      public static string Encrypt(string source, string[] passwords)
      {
         source = _encrypt(source, passwords);
         source = source.Sign();

         return source;
      }

      public static string Decrypt(string source, string[] passwords)
      {
         source = source.CheckSign();
         source = _decrypt(source, passwords);

         return source;
      }

      public static string Sign(this string source)
      {
         throw new NotImplementedException();
      }

      public static string CheckSign(this string source)
      {
         throw new NotImplementedException();
      }

      public static string Cipher_Aes(this string plainText, string key)
      {
         if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(plainText))
         {
            return plainText;
         }

         MD5 mD5 = MD5.Create();

         key = Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));
         key += Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));
         key += Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));

         byte[] _key = Encoding.UTF8.GetBytes(key[..32]);
         byte[] IV = Encoding.UTF8.GetBytes(key.Substring(32, 16));

         byte[] bytes = _cipher_Aes(plainText, _key, IV);

         return new string(bytes.Select(x => (char)x).ToArray());
      }

      private static byte[] _cipher_Aes(string plainText, byte[] key, byte[] IV)
      {
         using Aes aesAlg = Aes.Create();
         aesAlg.Key = key;
         aesAlg.IV = IV;

         ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

         using MemoryStream msEncrypt = new();
         using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
         using (StreamWriter swEncrypt = new(csEncrypt))
         {
            swEncrypt.Write(plainText);
         }

         return msEncrypt.ToArray();
      }

      public static string Uncipher_Aes(this string cipherText, string key)
      {
         if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(cipherText))
         {
            return cipherText;
         }

         MD5 mD5 = MD5.Create();
         key = Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));
         key += Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));
         key += Encoding.UTF8.GetString(mD5.ComputeHash(Encoding.UTF8.GetBytes(key)));

         byte[] _key = Encoding.UTF8.GetBytes(key[..32]);
         byte[] IV = Encoding.UTF8.GetBytes(key.Substring(32, 16));

         byte[] bytes = cipherText.Select(x => (byte)x).ToArray();

         return _uncither_Aes(bytes, _key, IV);
      }

      private static string _uncither_Aes(byte[] cipherText, byte[] key, byte[] IV)
      {
         using Aes aesAlg = Aes.Create();
         aesAlg.Key = key;
         aesAlg.IV = IV;

         ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

         using MemoryStream msDecrypt = new(cipherText);
         using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
         using StreamReader srDecrypt = new(csDecrypt);

         return srDecrypt.ReadToEnd();
      }

      private static string _encrypt(string source, string[] passwords)
      {
         for (int i = 0; i < passwords.Length; i++)
         {
            source = Cipher_Aes(source.Sign(), passwords[i]);
         }

         source = Cipher_Aes(source.Sign(), string.Empty.GetHash());

         return source;
      }

      private static string _decrypt(string source, string[] passwords)
      {
         try
         {
            source = Uncipher_Aes(source, string.Empty.GetHash());
            source = source.CheckSign();
         }
         catch
         {
            throw new CorruptedSourceException();
         }

         passwords = passwords.Reverse().ToArray();

         for (int i = passwords.Length - 1; i >= 0; i--)
         {
            try
            {
               source = Uncipher_Aes(source, passwords[i]);
               source = source.CheckSign();
            }
            catch
            {
               throw new WrongPasswordException(i);
            }
         }

         return source;
      }
   }
}
