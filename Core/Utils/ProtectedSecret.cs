using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   /// <summary>
   /// Holds a secret (an account password or a master passkey) encrypted in memory
   /// and only reveals it "just in time".
   ///
   /// The plaintext is never kept in a long-lived field: it lives encrypted for the
   /// whole session and a transient plaintext copy is produced only for the brief
   /// moment <see cref="Reveal"/> is called (e.g. to display, copy or re-encrypt).
   /// This shrinks the window during which a secret is exposed in the managed heap
   /// from "the entire session" down to "each individual use".
   ///
   /// The in-memory key is random, process-wide and never persisted, so a protected
   /// secret is worthless once the process ends. Persistence stores the revealed
   /// plaintext instead (the .pku onion encryption is what protects it at rest); see
   /// <see cref="ProtectedSecretJsonConverter"/>.
   /// </summary>
   internal sealed class ProtectedSecret
   {
      private const int KEY_SIZE = 32;
      private const int SALT_SIZE = 16;
      private const int NONCE_SIZE = 12;
      private const int TAG_SIZE = 16;

      // Random, process-wide key used to wrap every secret held in memory. It never
      // leaves RAM and dies with the process, so an in-memory secret cannot be
      // recovered from a persisted file or after the process exits. A fresh
      // per-secret key is still derived from it with HKDF (see below), so the same
      // key/nonce pair is never reused across two secrets.
      private static readonly byte[] _sessionKey = RandomNumberGenerator.GetBytes(KEY_SIZE);

      private readonly byte[] _protectedData;

      private ProtectedSecret(byte[] protectedData) => _protectedData = protectedData;

      /// <summary>
      /// Encrypts a secret so it can be held in memory without keeping its plaintext.
      /// </summary>
      public static ProtectedSecret Protect(string? secret)
      {
         byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
         byte[] nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);

         // A fresh AES-256 key per secret, derived from the session key and a random
         // salt, removes any nonce-reuse concern under a single long-lived key.
         byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256, _sessionKey, KEY_SIZE, salt);
         byte[] plainBytes = Encoding.Unicode.GetBytes(secret ?? string.Empty);

         try
         {
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TAG_SIZE];

            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // salt | nonce | tag | ciphertext, so Reveal is self-describing.
            return new ProtectedSecret([.. salt, .. nonce, .. tag, .. cipherBytes]);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
         }
      }

      /// <summary>
      /// Decrypts the secret just in time. The returned <see cref="string"/> is a
      /// short-lived plaintext copy that becomes eligible for garbage collection as
      /// soon as the caller stops referencing it; it should be used and dropped
      /// promptly rather than stored.
      /// </summary>
      public string Reveal()
      {
         ReadOnlySpan<byte> data = _protectedData;
         byte[] salt = data[..SALT_SIZE].ToArray();
         byte[] nonce = data.Slice(SALT_SIZE, NONCE_SIZE).ToArray();
         byte[] tag = data.Slice(SALT_SIZE + NONCE_SIZE, TAG_SIZE).ToArray();
         byte[] cipherBytes = data[(SALT_SIZE + NONCE_SIZE + TAG_SIZE)..].ToArray();

         byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256, _sessionKey, KEY_SIZE, salt);
         byte[] plainBytes = new byte[cipherBytes.Length];

         try
         {
            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return Encoding.Unicode.GetString(plainBytes);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
         }
      }

      // Never expose the secret through ToString: this prevents a protected value
      // from leaking into logs, debuggers or activity messages by accident.
      public override string ToString() => "***";
   }

   /// <summary>
   /// (De)serializes a <see cref="ProtectedSecret"/> as its plaintext string, so a
   /// persisted secret is a plain JSON string (protected at rest by the .pku onion
   /// encryption) while its in-memory representation stays encrypted. Deserializing
   /// immediately re-protects the value.
   /// </summary>
   internal sealed class ProtectedSecretJsonConverter : JsonConverter<ProtectedSecret>
   {
      public override ProtectedSecret Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
         => ProtectedSecret.Protect(reader.GetString());

      public override void Write(Utf8JsonWriter writer, ProtectedSecret value, JsonSerializerOptions options)
      {
         ArgumentNullException.ThrowIfNull(writer);
         ArgumentNullException.ThrowIfNull(value);

         writer.WriteStringValue(value.Reveal());
      }
   }
}
