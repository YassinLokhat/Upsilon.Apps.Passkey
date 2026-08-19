using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// BCL-only crypto: SHA-512 fingerprints, PBKDF2 stretching, AES-256-GCM
   /// onion encryption, and RSA-4096 hybrid encrypt / PSS sign. See SECURITY.md.
   /// </summary>
   public class CryptographyCenter : ICryptographyCenter
   {
      // Filename-safe SHA-512 (Base64 with '/' → '-'): used as the implicit first
      // onion layer (username) and as the .pku file stem next to the WPF binary.
      public string GetHash(string source) => Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(source))).Replace("/", "-", StringComparison.Ordinal);

      private const int SLOW_HASH_ITERATIONS = 1_000_000;
      private const int SLOW_HASH_SALT_SIZE = 16;

      // Floors for parameters read from an unencrypted header. Defaults sit well
      // above these; the floors follow the OWASP Password Storage Cheat Sheet
      // (PBKDF2-HMAC-SHA-256: 600k, PBKDF2-HMAC-SHA-512: 210k) so a hand-edited
      // or malicious .pku with Iterations = 1 cannot be used to stretch secrets.
      private const int MIN_SLOW_HASH_ITERATIONS_SHA256 = 600_000;
      private const int MIN_SLOW_HASH_ITERATIONS_SHA512 = 210_000;
      private const int MIN_SLOW_HASH_OUTPUT_LENGTH = 32;
      private const int MIN_SLOW_HASH_SALT_SIZE = 16;

      public KdfParameters DefaultSlowHashParameters => new()
      {
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

      public void EnsureSufficientSlowHashParameters(KdfParameters parameters)
      {
         ArgumentNullException.ThrowIfNull(parameters);

         int minIterations = parameters.Algorithm switch
         {
            KdfAlgorithm.Pbkdf2HmacSha256 => MIN_SLOW_HASH_ITERATIONS_SHA256,
            KdfAlgorithm.Pbkdf2HmacSha512 => MIN_SLOW_HASH_ITERATIONS_SHA512,
            _ => throw new InsufficientKdfParametersException(
               $"Unsupported KDF algorithm '{parameters.Algorithm}'."),
         };

         if (parameters.Iterations < minIterations)
         {
            throw new InsufficientKdfParametersException(
               $"KDF iterations '{parameters.Iterations}' for '{parameters.Algorithm}' are below the minimum of {minIterations}.");
         }

         if (parameters.OutputLength < MIN_SLOW_HASH_OUTPUT_LENGTH)
         {
            throw new InsufficientKdfParametersException(
               $"KDF output length '{parameters.OutputLength}' is below the minimum of {MIN_SLOW_HASH_OUTPUT_LENGTH} bytes.");
         }

         byte[] salt;
         try
         {
            salt = Convert.FromBase64String(parameters.Salt);
         }
         catch (FormatException ex)
         {
            throw new InsufficientKdfParametersException("KDF salt is not valid Base64.", ex);
         }

         if (salt.Length < MIN_SLOW_HASH_SALT_SIZE)
         {
            throw new InsufficientKdfParametersException(
               $"KDF salt length '{salt.Length}' is below the minimum of {MIN_SLOW_HASH_SALT_SIZE} bytes.");
         }
      }

      public string GetSlowHash(string source, KdfParameters parameters)
      {
         EnsureSufficientSlowHashParameters(parameters);

         byte[] salt = Convert.FromBase64String(parameters.Salt);
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
         ArgumentNullException.ThrowIfNull(passwords);

         // Onion encryption: every passkey adds an authenticated AES-GCM layer,
         // so all of them are required - and in the right order - to recover the
         // data. Layers are binary (salt | nonce | tag | ciphertext); Base64 is
         // applied once at the outer boundary so each layer does not inflate the
         // next by ~4/3.
         byte[] result = Encoding.UTF8.GetBytes(source);

         for (int i = passwords.Length - 1; i >= 0; i--)
         {
            result = _encryptGcmLayerBytes(result, passwords[i]);
         }

         // A final layer keyed with a fixed, public value lets decryption tell
         // "corrupted or foreign data" apart from "valid data, wrong passkey".
         result = _encryptGcmLayerBytes(result, GetHash(string.Empty));

         return Convert.ToBase64String(result);
      }

      public string DecryptSymmetrically(string source, string[] passwords)
      {
         ArgumentNullException.ThrowIfNull(passwords);

         byte[] result;

         try
         {
            result = Convert.FromBase64String(source);
            result = _decryptGcmLayerBytes(result, GetHash(string.Empty));
         }
         catch
         {
            throw new CorruptedSourceException();
         }

         for (int i = 0; i < passwords.Length; i++)
         {
            try
            {
               result = _decryptGcmLayerBytes(result, passwords[i]);
            }
            catch
            {
               throw new WrongPasswordException(i);
            }
         }

         return Encoding.UTF8.GetString(result);
      }

      public void GenerateRandomKeys(out string publicKey, out string privateKey)
      {
         using RSA rsa = RSA.Create(4096);

         privateKey = rsa.ExportRSAPrivateKeyPem();
         publicKey = rsa.ExportRSAPublicKeyPem();
      }

      public string EncryptAsymmetrically(string source, string key)
      {
         // One-time AES key for the payload; RSA-OAEP wraps only that key so
         // activity rows can be written with the public key alone.
         string aesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(KEY_SIZE));
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
         catch (Exception ex)
            when (ex is ArgumentNullException
            || ex is ArgumentException
            || ex is FormatException
            || ex is CryptographicException)
         {
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
      {
         byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

         try
         {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, passwordBytes, KEY_SIZE, salt);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(passwordBytes);
         }
      }

      private static byte[] _encryptGcmLayerBytes(ReadOnlySpan<byte> plainBytes, string password)
      {
         byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
         byte[] nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);
         byte[] key = _deriveLayerKey(password, salt);
         byte[] cipherBytes = new byte[plainBytes.Length];
         byte[] tag = new byte[TAG_SIZE];

         try
         {
            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // salt | nonce | tag | ciphertext, so decryption is self-describing.
            return [.. salt, .. nonce, .. tag, .. cipherBytes];
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
         }
      }

      private static byte[] _decryptGcmLayerBytes(ReadOnlySpan<byte> payload, string password)
      {
         if (payload.Length < SALT_SIZE + NONCE_SIZE + TAG_SIZE)
         {
            throw new CryptographicException("Ciphertext is too short to be valid.");
         }

         ReadOnlySpan<byte> salt = payload[..SALT_SIZE];
         ReadOnlySpan<byte> nonce = payload.Slice(SALT_SIZE, NONCE_SIZE);
         ReadOnlySpan<byte> tag = payload.Slice(SALT_SIZE + NONCE_SIZE, TAG_SIZE);
         ReadOnlySpan<byte> cipherBytes = payload[(SALT_SIZE + NONCE_SIZE + TAG_SIZE)..];

         byte[] key = _deriveLayerKey(password, salt.ToArray());
         byte[] plainBytes = new byte[cipherBytes.Length];

         try
         {
            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return plainBytes;
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
