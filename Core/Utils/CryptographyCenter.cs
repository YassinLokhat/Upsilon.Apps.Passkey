using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   public class CryptographyCenter : ICryptographyCenter
   {
      public string GetHash(string source) => Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(source))).Replace("/", "-", StringComparison.Ordinal);

      private const int SLOW_HASH_ITERATIONS = 1_000_000;
      private const int SLOW_HASH_SALT_SIZE = 16;

      public KdfParameters DefaultSlowHashParameters => new()
      {
         Version = 1,
         // HMAC-SHA-512 relies on 64-bit arithmetic, which GPUs and ASICs run
         // far less efficiently than the 32-bit operations of SHA-256. At an
         // equal iteration count this narrows an attacker's parallel-hardware
         // advantage for offline guessing, while staying within the .NET BCL.
         Algorithm = KdfAlgorithm.Pbkdf2HmacSha512,
         Iterations = SLOW_HASH_ITERATIONS,
         OutputLength = 64,
         // A fresh 128-bit random salt is minted for every new database, so two
         // databases (even with the same username and passkeys) never stretch to
         // the same key material. It is stored, unencrypted, in the header; a
         // salt is not secret. Each access mints a new salt, so the returned
         // instance must be captured once per database rather than re-read.
         Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SLOW_HASH_SALT_SIZE)),
      };

      public string GetSlowHash(string source, KdfParameters parameters)
      {
         ArgumentNullException.ThrowIfNull(parameters);

         // The salt is a random, per-database value carried in the parameters
         // (read back from the header), so it is well-formed by construction and
         // stable for the life of the file, which is required to reopen it.
         byte[] salt = Convert.FromBase64String(parameters.Salt);

         // PBKDF2 is a standard password-stretching KDF. The exact algorithm,
         // work factor and salt are taken from the caller so that a database can
         // be reopened with the parameters it was written with (crypto-agility).
         byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(source),
            salt,
            parameters.Iterations,
            _toHashAlgorithmName(parameters.Algorithm),
            parameters.OutputLength);

         return Convert.ToBase64String(hash);
      }

      private static HashAlgorithmName _toHashAlgorithmName(KdfAlgorithm algorithm) => algorithm switch
      {
         KdfAlgorithm.Pbkdf2HmacSha256 => HashAlgorithmName.SHA256,
         KdfAlgorithm.Pbkdf2HmacSha512 => HashAlgorithmName.SHA512,
         _ => throw new NotSupportedException($"Unsupported KDF algorithm '{algorithm}'."),
      };

      public int HashLength => GetHash(string.Empty).Length;

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

         // The payload is sealed with authenticated AES-GCM and the AES key is
         // wrapped with RSA-OAEP, so both parts already detect tampering - no
         // separate signature is needed over the envelope.
         source = EncryptSymmetrically(source, [aesKey]);
         aesKey = _encryptRsa(aesKey, key);
         KeyValuePair<string, string> s = new(aesKey, source);

         return JsonSerializer.Serialize(s);
      }

      public string DecryptAsymmetrically(string source, string key)
      {
         KeyValuePair<string, string> s;

         try
         {
            s = JsonSerializer.Deserialize<KeyValuePair<string, string>>(source);
         }
         catch (JsonException)
         {
            throw new CorruptedSourceException();
         }

         // A wrong key fails the RSA unwrap (WrongPasswordException); any
         // tampering with the wrapped key or the payload is caught by RSA-OAEP
         // or the AES-GCM tag inside DecryptSymmetrically.
         string aesKey = _decryptRsa(s.Key, key);

         return DecryptSymmetrically(s.Value, [aesKey]);
      }

      public string GetPublicKey(string privateKey)
      {
         using RSA rsa = RSA.Create();
         rsa.ImportFromPem(privateKey);

         return rsa.ExportRSAPublicKeyPem();
      }

      public string Sign(string source, string privateKey)
      {
         using RSA rsa = RSA.Create();
         rsa.ImportFromPem(privateKey);

         // RSA-PSS with SHA-256 is the modern, randomized signature scheme
         // (preferred over the legacy PKCS#1 v1.5 padding). SignData hashes the
         // input itself, so an arbitrarily long payload can be signed directly.
         byte[] signature = rsa.SignData(Encoding.UTF8.GetBytes(source), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

         return Convert.ToBase64String(signature);
      }

      public bool Verify(string source, string signature, string publicKey)
      {
         try
         {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            return rsa.VerifyData(Encoding.UTF8.GetBytes(source),
               Convert.FromBase64String(signature),
               HashAlgorithmName.SHA256,
               RSASignaturePadding.Pss);
         }
         catch
         {
            // Any malformed key/signature (or a mismatch) is treated as an
            // invalid signature rather than surfacing as an exception.
            return false;
         }
      }

      private const int SALT_SIZE = 16;
      private const int NONCE_SIZE = 12;
      private const int TAG_SIZE = 16;
      private const int KEY_SIZE = 32;

      // The passkeys reaching this layer are already high-entropy values
      // (slow-hashed master passwords or a random AES key), so HKDF is the
      // right tool to expand them into a fresh AES-256 key. Brute-force
      // hardening of human-chosen passwords belongs to GetSlowHash, not here.
      private static byte[] _deriveLayerKey(string password, byte[] salt)
         => HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(password), KEY_SIZE, salt);

      private static string _encryptGcmLayer(string plainText, string password)
      {
         byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
         byte[] nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);
         byte[] key = _deriveLayerKey(password, salt);

         try
         {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TAG_SIZE];

            using (AesGcm aesGcm = new(key, TAG_SIZE))
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

         if (data.Length < SALT_SIZE + NONCE_SIZE + TAG_SIZE)
         {
            throw new CryptographicException("Ciphertext is too short to be valid.");
         }

         ReadOnlySpan<byte> dataSpan = data;
         byte[] salt = dataSpan[..SALT_SIZE].ToArray();
         byte[] nonce = dataSpan.Slice(SALT_SIZE, NONCE_SIZE).ToArray();
         byte[] tag = dataSpan.Slice(SALT_SIZE + NONCE_SIZE, TAG_SIZE).ToArray();
         byte[] cipherBytes = dataSpan[(SALT_SIZE + NONCE_SIZE + TAG_SIZE)..].ToArray();

         byte[] key = _deriveLayerKey(password, salt);

         try
         {
            byte[] plainBytes = new byte[cipherBytes.Length];

            // AES-GCM verifies the tag while decrypting and throws on any
            // tampering or wrong key, which is how callers detect both.
            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
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

         byte[] bytesPlainTextData = Encoding.UTF8.GetBytes(source);
         byte[] bytesCypherText = rsa.Encrypt(bytesPlainTextData, RSAEncryptionPadding.OaepSHA256);

         source = Convert.ToBase64String(bytesCypherText);

         return source;
      }

      private static string _decryptRsa(string source, string privateKeyPem)
      {
         try
         {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] bytesCypherText = Convert.FromBase64String(source);
            byte[] bytesPlainTextData = rsa.Decrypt(bytesCypherText, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(bytesPlainTextData);
         }
         catch
         {
            throw new WrongPasswordException(0);
         }
      }
   }
}
