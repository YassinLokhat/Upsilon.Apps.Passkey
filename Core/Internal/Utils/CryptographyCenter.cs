using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Internal.Utils
{
   /// <summary>
   /// Represent a cryptographic center Internal.
   /// </summary>
   public class CryptographyCenter : ICryptographyCenter
   {
      /// <summary>
      /// Returs a fast string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      public string GetHash(string source)
      {
         string md5Hash = Convert.ToBase64String(MD5.HashData(Encoding.Unicode.GetBytes(source))).TrimEnd('=');
         string sha1Hash = Convert.ToBase64String(SHA1.HashData(Encoding.Unicode.GetBytes(source))).TrimEnd('=');

         return md5Hash + sha1Hash;
      }

      /// <summary>
      /// Returs a slow string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      public string GetSlowHash(string source)
      {
         long realTimeFactor = (long)Math.Pow(0b1000, 5);

         for (int i = 0; i < realTimeFactor; i++)
         {
            source = GetHash(source);
         }

         return source;
      }

      /// <summary>
      /// The fixed length of the hash.
      /// </summary>
      public int HashLength => GetHash(string.Empty).Length;

      /// <summary>
      /// Sign a string.
      /// </summary>
      /// <param name="source">The string to sign. The method will modify the string to add the signature.</param>
      public void Sign(ref string source)
      {
         source = GetHash(source) + source;
      }

      /// <summary>
      /// check the signature of a given string.
      /// </summary>
      /// <param name="source">The string to sign. The method will modify the string to remove the signature.</param>
      /// <returns>True if the signature is good, False else.</returns>
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

      /// <summary>
      /// Encrypt symmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The encrypted string.</returns>
      public string EncryptSymmetrically(string source, string[] passwords)
      {
         source = _encryptAes(source, passwords);
         source = Convert.ToBase64String(Encoding.Unicode.GetBytes(source));

         Sign(ref source);

         return source;
      }

      /// <summary>
      /// Decrypt symmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The decrypted string.</returns>
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

      /// <summary>
      /// Generate a random public key and private key pair.
      /// </summary>
      /// <param name="publicKey">The public key generated.</param>
      /// <param name="privateKey">The private key generated.</param>
      public void GenerateRandomKeys(out string publicKey, out string privateKey)
      {
         RSACryptoServiceProvider csp = new(2048);

         StringWriter sw = new();
         System.Xml.Serialization.XmlSerializer xs = new(typeof(RSAParameters));

         xs.Serialize(sw, csp.ExportParameters(includePrivateParameters: false));
         publicKey = sw.ToString();

         sw = new System.IO.StringWriter();
         xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

         xs.Serialize(sw, csp.ExportParameters(includePrivateParameters: true));
         privateKey = sw.ToString();
      }

      /// <summary>
      /// Encrypt asymmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="key">The encryption key.</param>
      /// <returns>The encrypted string.</returns>
      public string EncryptAsymmetrically(string source, string key)
      {
         RSACryptoServiceProvider csp = new();

         StringReader sr = new(key);
         System.Xml.Serialization.XmlSerializer xs = new(typeof(RSAParameters));

         RSAParameters pubKey = (RSAParameters?)xs.Deserialize(sr) ?? throw new WrongPasswordException(0);

         csp.ImportParameters(pubKey);
         StringBuilder sb = new();

         while (source.Length != 0)
         {
            int size = source.Length < 100 ? source.Length : 100;

            _ = sb.Append(_encryptRsa(source[..size], csp) + "|");

            source = source[size..];
         }

         source = sb.ToString().TrimEnd('|');

         Sign(ref source);

         return source;
      }

      /// <summary>
      /// Decrypt asymmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="key">The encryption key.</param>
      /// <returns>The decrypted string.</returns>
      public string DecryptAsymmetrically(string source, string key)
      {
         if (!CheckSign(ref source))
         {
            throw new CheckSignFailedException();
         }

         RSACryptoServiceProvider csp = new();

         StringReader sr = new(key);
         System.Xml.Serialization.XmlSerializer xs = new(typeof(RSAParameters));

         RSAParameters privKey = (RSAParameters?)xs.Deserialize(sr) ?? throw new Exception();

         csp.ImportParameters(privKey);

         string[] sourecs = source.Split('|');
         StringBuilder sb = new();

         for (int i = 0; i < sourecs.Length; i++)
         {
            _ = sb.Append(_decryptRsa(sourecs[i], i, csp));
         }

         return sb.ToString();
      }

      private string _cipherAes(string plainText, string key)
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

         byte[] bytes = _cipherAes(plainText, _key, IV);

         return new string(bytes.Select(x => (char)x).ToArray());
      }

      private byte[] _cipherAes(string plainText, byte[] key, byte[] IV)
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

      private string _uncipherAes(string cipherText, string key)
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

         return _uncitherAes(bytes, _key, IV);
      }

      private string _uncitherAes(byte[] cipherText, byte[] key, byte[] IV)
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
         passwords = passwords.Select(x => GetHash(x)).ToArray();

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
         passwords = passwords.Select(x => GetHash(x)).ToArray();

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

      private string _encryptRsa(string source, RSACryptoServiceProvider csp)
      {
         byte[] bytesPlainTextData = Encoding.Unicode.GetBytes(source);
         byte[] bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

         source = Convert.ToBase64String(bytesCypherText);

         return source;
      }

      private string _decryptRsa(string source, int level, RSACryptoServiceProvider csp)
      {
         try
         {
            byte[] bytesCypherText = Convert.FromBase64String(source);
            byte[] bytesPlainTextData = csp.Decrypt(bytesCypherText, false);

            return Encoding.Unicode.GetString(bytesPlainTextData);
         }
         catch
         {
            throw new WrongPasswordException(level);
         }
      }
   }
}
