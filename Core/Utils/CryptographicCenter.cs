﻿using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   /// <summary>
   /// Represent a cryptographic center implementation.
   /// </summary>
   public class CryptographicCenter : ICryptographicCenter
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
      /// Encrypt a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The encrypted string.</returns>
      public string Encrypt(string source, string[] passwords)
      {
         source = _encrypt(source, passwords);
         source = _stringToCustomBase(source);

         Sign(ref source);

         return source;
      }

      /// <summary>
      /// Decrypt a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The decrypted string.</returns>
      public string Decrypt(string source, string[] passwords)
      {
         if (!CheckSign(ref source))
         {
            throw new CheckSignFailedException();
         }

         source = _customBaseToString(source);
         source = _decrypt(source, passwords);

         return source;
      }

      private string _cipher_Aes(string plainText, string key)
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

      private byte[] _cipher_Aes(string plainText, byte[] key, byte[] IV)
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

      private string _uncipher_Aes(string cipherText, string key)
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

      private string _uncither_Aes(byte[] cipherText, byte[] key, byte[] IV)
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

      private string _encrypt(string source, string[] passwords)
      {
         passwords = passwords.Select(x => GetHash(x)).ToArray();

         for (int i = 0; i < passwords.Length; i++)
         {
            Sign(ref source);
            source = _cipher_Aes(source, passwords[i]);
         }

         Sign(ref source);
         source = _cipher_Aes(source, GetHash(string.Empty));

         return source;
      }

      private string _decrypt(string source, string[] passwords)
      {
         passwords = passwords.Select(x => GetHash(x)).ToArray();

         try
         {
            source = _uncipher_Aes(source, GetHash(string.Empty));
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
               source = _uncipher_Aes(source, passwords[i]);

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
