using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class CryptographyCenter : ICryptographyCenter
   {
      public string GetHash(string source) => Convert.ToBase64String(SHA512.HashData(Encoding.Unicode.GetBytes(source))).Replace("/", "-");

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
         // Onion encryption: every passkey adds an authenticated AES-GCM layer,
         // so all of them are required - and in the right order - to recover the
         // data.
         string result = source;

         for (int i = passwords.Length - 1; i >= 0; i--)
         {
            result = _encryptGcmLayer(result, passwords[i]);
         }

         // A final layer keyed with a fixed, public value lets decryption tell
         // "corrupted or foreign data" apart from "valid data, wrong passkey".
         return _encryptGcmLayer(result, GetHash(string.Empty));
      }

      public string DecryptSymmetrically(string source, string[] passwords)
      {
         string result;

         try
         {
            result = _decryptGcmLayer(source, GetHash(string.Empty));
         }
         catch
         {
            throw new CorruptedSourceException();
         }

         for (int i = 0; i < passwords.Length; i++)
         {
            try
            {
               result = _decryptGcmLayer(result, passwords[i]);
            }
            catch
            {
               throw new WrongPasswordException(i);
            }
         }

         return result;
      }

      public void GenerateRandomKeys(out string publicKey, out string privateKey)
      {
         using RSA rsa = RSA.Create(4096);

         privateKey = rsa.ExportRSAPrivateKeyPem();
         publicKey = rsa.ExportRSAPublicKeyPem();
      }

      public string EncryptAsymmetrically(string source, string key)
      {
         // The one-time AES key wraps the payload while the RSA layer protects
         // the key itself. It must be unpredictable, so it is drawn from a
         // CSPRNG and Base64-encoded to keep every bit of entropy (encoding raw
         // random bytes as UTF-8 would silently drop invalid sequences).
         byte[] randomBytes = RandomNumberGenerator.GetBytes(100);
         string aesKey = Convert.ToBase64String(randomBytes);
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

      private const int _saltSize = 16;
      private const int _nonceSize = 12;
      private const int _tagSize = 16;
      private const int _keySize = 32;

      // The passkeys reaching this layer are already high-entropy values
      // (slow-hashed master passwords or a random AES key), so HKDF is the
      // right tool to expand them into a fresh AES-256 key. Brute-force
      // hardening of human-chosen passwords belongs to GetSlowHash, not here.
      private static byte[] _deriveLayerKey(string password, byte[] salt)
         => HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.Unicode.GetBytes(password), _keySize, salt);

      private static string _encryptGcmLayer(string plainText, string password)
      {
         byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);
         byte[] nonce = RandomNumberGenerator.GetBytes(_nonceSize);
         byte[] key = _deriveLayerKey(password, salt);

         try
         {
            byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[_tagSize];

            using (AesGcm aesGcm = new(key, _tagSize))
            {
               aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // salt | nonce | tag | ciphertext, so decryption is self-describing.
            return Convert.ToBase64String([.. salt, .. nonce, .. tag, .. cipherBytes]);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
         }
      }

      private static string _decryptGcmLayer(string payload, string password)
      {
         byte[] data = Convert.FromBase64String(payload);

         if (data.Length < _saltSize + _nonceSize + _tagSize)
         {
            throw new CryptographicException("Ciphertext is too short to be valid.");
         }

         ReadOnlySpan<byte> dataSpan = data;
         byte[] salt = dataSpan[.._saltSize].ToArray();
         byte[] nonce = dataSpan.Slice(_saltSize, _nonceSize).ToArray();
         byte[] tag = dataSpan.Slice(_saltSize + _nonceSize, _tagSize).ToArray();
         byte[] cipherBytes = dataSpan[(_saltSize + _nonceSize + _tagSize)..].ToArray();

         byte[] key = _deriveLayerKey(password, salt);

         try
         {
            byte[] plainBytes = new byte[cipherBytes.Length];

            // AES-GCM verifies the tag while decrypting and throws on any
            // tampering or wrong key, which is how callers detect both.
            using (AesGcm aesGcm = new(key, _tagSize))
            {
               aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return Encoding.Unicode.GetString(plainBytes);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
         }
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
