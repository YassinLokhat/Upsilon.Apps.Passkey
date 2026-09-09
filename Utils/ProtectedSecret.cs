using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Utils
{
   /// <summary>
   /// Holds a secret encrypted in memory and reveals it just in time via <see cref="Reveal"/>.
   /// Persistence uses plaintext inside the .pku onion; see <see cref="ProtectedSecretJsonConverter"/>.
   /// </summary>
   public sealed class ProtectedSecret : IProtectedSecret
   {
      private const int KEY_SIZE = 32;
      private const int SALT_SIZE = 16;
      private const int NONCE_SIZE = 12;
      private const int TAG_SIZE = 16;

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

         byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256, _sessionKey, KEY_SIZE, salt);
         byte[] plainBytes = Encoding.UTF8.GetBytes(secret ?? string.Empty);

         try
         {
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TAG_SIZE];

            using (AesGcm aesGcm = new(key, TAG_SIZE))
            {
               aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // Layout: salt | nonce | tag | ciphertext
            return new ProtectedSecret([.. salt, .. nonce, .. tag, .. cipherBytes]);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
         }
      }

      /// <summary>
      /// Decrypts the secret just in time. Drop the returned string promptly; do not store it.
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

            return Encoding.UTF8.GetString(plainBytes);
         }
         finally
         {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
         }
      }

      public override string ToString() => "***";
   }

   /// <summary>
   /// Default <see cref="ISecretMemoryProtector"/> using <see cref="ProtectedSecret"/>.
   /// </summary>
   public sealed class SecretMemoryProtector : ISecretMemoryProtector
   {
      public IProtectedSecret Protect(string? secret) => ProtectedSecret.Protect(secret);
   }

   /// <summary>
   /// JSON wire format is plaintext (onion-protected at rest); deserializing re-protects in memory.
   /// </summary>
   public sealed class ProtectedSecretJsonConverter : JsonConverter<IProtectedSecret>
   {
      public override IProtectedSecret Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
         => ProtectedSecret.Protect(reader.GetString());

      public override void Write(Utf8JsonWriter writer, IProtectedSecret value, JsonSerializerOptions options)
      {
         ArgumentNullException.ThrowIfNull(writer);
         ArgumentNullException.ThrowIfNull(value);

         writer.WriteStringValue(value.Reveal());
      }
   }

   /// <summary>
   /// Same wire format as <see cref="ProtectedSecretJsonConverter"/> for the concrete type.
   /// </summary>
   public sealed class ProtectedSecretConcreteJsonConverter : JsonConverter<ProtectedSecret>
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
