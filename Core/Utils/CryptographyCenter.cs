using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class CryptographyCenter : ICryptographyCenter
   {
      public string GetHash(string source)
      {
         string md5Hash = Convert.ToBase64String(MD5.HashData(Encoding.Unicode.GetBytes(source)));
         string sha1Hash = Convert.ToBase64String(SHA1.HashData(Encoding.Unicode.GetBytes(source)));

         return (md5Hash + sha1Hash).Replace("/", "-");
      }

      public string GetSlowHash(string source)
      {
         long realTimeFactor = (long)Math.Pow(0b1001, 6);

         for (int i = 0; i < realTimeFactor; i++)
         {
            source = GetHash(source);
         }

         return source;
      }

      public int HashLength => GetHash(string.Empty).Length;

      public void Sign(ref string source)
      {
         source = GetHash(source) + source;
      }

      public bool CheckSign(ref string source)
      {
         try
         {
            string hashSource = source[..HashLength];
            string hashCheck = GetHash(source[HashLength..]);

            if (hashSource != hashCheck)
            {
               throw new Exception();
            }

            source = source[HashLength..];
         }
         catch
         {
            return false;
         }

         return true;
      }

      public string EncryptSymmetrically(string source, string[] passwords)
      {
         source = _encryptAes(source, passwords);
         source = Convert.ToBase64String(Encoding.Unicode.GetBytes(source));

         Sign(ref source);

         return source;
      }

      public string DecryptSymmetrically(string source, string[] passwords)
      {
         if (!CheckSign(ref source))
         {
            throw new CheckSignFailedException();
         }

         source = Encoding.Unicode.GetString(Convert.FromBase64String(source));
         source = _decryptAes(source, passwords);

         return source;
      }

      public void GenerateRandomKeys(out string publicKey, out string privateKey)
      {
         using RSA rsa = RSA.Create(4096);

         privateKey = rsa.ExportRSAPrivateKeyPem();
         publicKey = rsa.ExportRSAPublicKeyPem();
      }

      public string EncryptAsymmetrically(string source, string key)
      {
         Random random = new((int)DateTime.Now.Ticks);
         byte[] randomBytes = new byte[100];
         random.NextBytes(randomBytes);
         string aesKey = Encoding.UTF8.GetString(randomBytes);
         source = EncryptSymmetrically(source, [aesKey]);
         aesKey = _encryptRsa(aesKey, key);
         KeyValuePair<string, string> s = new(aesKey, source);
         source = JsonSerializer.Serialize(s);

         Sign(ref source);

         return source;
      }

      public string DecryptAsymmetrically(string source, string key)
      {
         if (!CheckSign(ref source))
         {
            throw new CheckSignFailedException();
         }

         KeyValuePair<string, string> s = JsonSerializer.Deserialize<KeyValuePair<string, string>>(source);
         string aesKey = _decryptRsa(s.Key, 0, key);
         source = DecryptSymmetrically(s.Value, [aesKey]);

         return source;
      }

      private static string _cipherAes(string plainText, string key)
      {
         if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(plainText))
         {
            return plainText;
         }

         key = Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));

         byte[] _key = Encoding.ASCII.GetBytes(key[..32]);
         byte[] IV = Encoding.ASCII.GetBytes(key.Substring(32, 16));

         byte[] bytes = _cipherAes(plainText, _key, IV);

         return new string([.. bytes.Select(x => (char)x)]);
      }

      private static byte[] _cipherAes(string plainText, byte[] key, byte[] IV)
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

      private static string _uncipherAes(string cipherText, string key)
      {
         if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(cipherText))
         {
            return cipherText;
         }
         key = Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));
         key += Encoding.ASCII.GetString(MD5.HashData(Encoding.ASCII.GetBytes(key)));

         byte[] _key = Encoding.ASCII.GetBytes(key[..32]);
         byte[] IV = Encoding.ASCII.GetBytes(key.Substring(32, 16));

         byte[] bytes = [.. cipherText.Select(x => (byte)x)];

         return _uncitherAes(bytes, _key, IV);
      }

      private static string _uncitherAes(byte[] cipherText, byte[] key, byte[] IV)
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

      private string _encryptAes(string source, string[] passwords)
      {
         passwords = [.. passwords.Select(GetHash)];

         for (int i = passwords.Length - 1; i >= 0; i--)
         {
            Sign(ref source);
            source = _cipherAes(source, passwords[i]);
         }

         Sign(ref source);
         source = _cipherAes(source, GetHash(string.Empty));

         return source;
      }

      private string _decryptAes(string source, string[] passwords)
      {
         passwords = [.. passwords.Select(GetHash)];

         try
         {
            source = _uncipherAes(source, GetHash(string.Empty));
         }
         catch
         {
            throw new CorruptedSourceException();
         }

         if (!CheckSign(ref source))
         {
            throw new CheckSignFailedException();
         }

         for (int i = 0; i < passwords.Length; i++)
         {
            try
            {
               source = _uncipherAes(source, passwords[i]);

               if (!CheckSign(ref source))
               {
                  throw new CheckSignFailedException();
               }
            }
            catch
            {
               throw new WrongPasswordException(i);
            }
         }

         return source;
      }

      private static string _encryptRsa(string source, string publicKeyPem)
      {
         using RSA rsa = RSA.Create();
         rsa.ImportFromPem(publicKeyPem);

         byte[] bytesPlainTextData = Encoding.Unicode.GetBytes(source);
         byte[] bytesCypherText = rsa.Encrypt(bytesPlainTextData, RSAEncryptionPadding.OaepSHA256);

         source = Convert.ToBase64String(bytesCypherText);

         return source;
      }

      private static string _decryptRsa(string source, int level, string privateKeyPem)
      {
         try
         {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] bytesCypherText = Convert.FromBase64String(source);
            byte[] bytesPlainTextData = rsa.Decrypt(bytesCypherText, RSAEncryptionPadding.OaepSHA256);
            return Encoding.Unicode.GetString(bytesPlainTextData);
         }
         catch
         {
            throw new WrongPasswordException(level);
         }
      }
   }
}
