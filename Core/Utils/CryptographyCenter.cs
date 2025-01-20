using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   /// <summary>
   /// Represent a cryptographic center implementation.
   /// </summary>
   public class CryptographyCenter : ICryptographyCenter
   {
      private readonly string _alphabet = "BT2Cp4oOU-DqinLjy0HWxk8wI9rY1QgXblaef5RtdFE3sGm6PSzMJvKVhu7+NcZA";
      private readonly string _hexadecimal = "0123456789ABCDEF";

      /// <summary>
      /// Returs a fast string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      public string GetHash(string source)
      {
         MD5 md5 = MD5.Create();

         IEnumerable<string> hash = md5
            .ComputeHash(Encoding.UTF8.GetBytes(source))
            .Select(x => x.ToString("X2"));

         return string.Join(string.Empty, hash).ToLower();
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
         source = _stringToCustomBase(source);

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

         source = _customBaseToString(source);
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

         var sw = new System.IO.StringWriter();
         var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

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

         var sr = new System.IO.StringReader(key);
         var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

         RSAParameters pubKey = (RSAParameters?)xs.Deserialize(sr) ?? throw new WrongPasswordException(0);

         csp.ImportParameters(pubKey);

         var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(source);
         var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

         source = Convert.ToBase64String(bytesCypherText);

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

         try
         {
            RSACryptoServiceProvider csp = new();

            var sr = new System.IO.StringReader(key);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

            RSAParameters privKey = (RSAParameters?)xs.Deserialize(sr) ?? throw new Exception();

            csp.ImportParameters(privKey);

            var bytesCypherText = Convert.FromBase64String(source);
            var bytesPlainTextData = csp.Decrypt(bytesCypherText, false);

            return System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
         }
         catch
         {
            throw new WrongPasswordException(0);
         }
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

         for (int i = 0; i < passwords.Length; i++)
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

         for (int i = passwords.Length - 1; i >= 0; i--)
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

      private string _stringToCustomBase(string source)
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

      private string _customBaseToString(string source)
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
