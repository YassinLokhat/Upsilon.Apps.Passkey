using System.Security.Cryptography;
using System.Text;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   public static class SecurityCenter
   {
      private static readonly string _alphabet = "BT2Cp4oOU-DqinLjy0HWxk8wI9rY1QgXblaef5RtdFE3sGm6PSzMJvKVhu7+NcZA";
      private static readonly string _hexadecimal = "0123456789ABCDEF";

      public static string GetHash(this string source)
      {
         MD5 md5 = MD5.Create();

         IEnumerable<string> hash = md5
            .ComputeHash(Encoding.UTF8.GetBytes(source))
            .Select(x => x.ToString("X2"));

         return string.Join(string.Empty, hash);
      }

      public static string GetSlowHash(this string source, byte timeFactor = 5)
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
         source = _stringToCustomBase(source);
         source = source.Sign();

         return source;
      }

      public static string Decrypt(string source, string[] passwords)
      {
         source = source.CheckSign();
         source = _customBaseToString(source);
         source = _decrypt(source, passwords);

         return source;
      }

      public static string Sign(this string source)
      {
         return source.GetHash() + source;
      }

      public static string CheckSign(this string source)
      {
         try
         {
            string hashSource = source[..HashLength];
            string hashCheck = source[HashLength..].GetHash();

            if (hashSource != hashCheck)
            {
               throw new Exception();
            }

            source = source[HashLength..];
         }
         catch
         {
            throw new CheckSignFailedException();
         }

         return source;
      }

      public static string Cipher_Aes(this string plainText, string key)
      {
         if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(plainText))
         {
            return plainText;
         }

         MD5 mD5 = MD5.Create();

         key = Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));

         byte[] _key = Encoding.ASCII.GetBytes(key[..32]);
         byte[] IV = Encoding.ASCII.GetBytes(key.Substring(32, 16));

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
         key = Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(mD5.ComputeHash(Encoding.ASCII.GetBytes(key)));

         byte[] _key = Encoding.ASCII.GetBytes(key[..32]);
         byte[] IV = Encoding.ASCII.GetBytes(key.Substring(32, 16));

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
         passwords = passwords.Select(x => x.GetHash()).ToArray();

         for (int i = 0; i < passwords.Length; i++)
         {
            source = Cipher_Aes(source.Sign(), passwords[i]);
         }

         source = Cipher_Aes(source.Sign(), string.Empty.GetHash());

         return source;
      }

      private static string _decrypt(string source, string[] passwords)
      {
         passwords = passwords.Select(x => x.GetHash()).ToArray();

         try
         {
            source = Uncipher_Aes(source, string.Empty.GetHash());
            source = source.CheckSign();
         }
         catch
         {
            throw new CorruptedSourceException();
         }

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

      private static string _stringToCustomBase(string source)
      {
         StringBuilder hexaHigh = new(), hexaLow = new();
         byte[] bytes = Encoding.UTF8.GetBytes(source);
         int seed = 0;

         foreach (byte b in bytes)
         {
            string hexa = b.ToString("X2");
            int index = _hexadecimal.IndexOf(hexa[0]) + (_hexadecimal.Length * seed);
            _ = hexaHigh.Append(_alphabet[index]);
            index = _hexadecimal.IndexOf(hexa[1]) + (_hexadecimal.Length * seed);
            _ = hexaLow.Append(_alphabet[index]);
            seed = b % 3;
         }

         return hexaHigh.ToString() + hexaLow.ToString();
      }

      private static string _customBaseToString(string source)
      {
         List<byte> bytes = [];
         int bytesCount = source.Length / 2;

         for (int i = 0; i < bytesCount; i++)
         {
            int indexHigh = _alphabet.IndexOf(source[i]) % _hexadecimal.Length;
            int indexLow = _alphabet.IndexOf(source[i + bytesCount]) % _hexadecimal.Length;

            if (indexLow == -1 ||
                indexHigh == -1)
            {
               throw new CorruptedSourceException();
            }

            string hexa = $"{_hexadecimal[indexHigh]}{_hexadecimal[indexLow]}";
            bytes.Add(Convert.ToByte(hexa, 16));
         }

         return Encoding.UTF8.GetString(bytes.ToArray());
      }
   }
}
