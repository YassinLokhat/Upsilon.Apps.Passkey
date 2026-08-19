namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Hashing, onion encryption, and RSA hybrid encrypt / sign.
   /// </summary>
   public interface ICryptographyCenter
   {
      /// <summary>
      /// Fast SHA-512 fingerprint, Base64 with <c>/</c> replaced by <c>-</c> so
      /// the value can be used in a file name (e.g. <c>raw/{hash}.pku</c>).
      /// Not a password KDF — use <see cref="GetSlowHash"/> for passkeys.
      /// </summary>
      string GetHash(string source);

      /// <summary>
      /// Stretch a passkey with <paramref name="parameters"/> (floor-checked first).
      /// </summary>
      /// <exception cref="InsufficientKdfParametersException">
      /// Parameters fall below the accepted floor or are malformed.
      /// </exception>
      string GetSlowHash(string source, KdfParameters parameters);

      /// <summary>
      /// Reject weakened or malformed KDF headers before stretching passkeys.
      /// </summary>
      /// <exception cref="InsufficientKdfParametersException">
      /// Parameters fall below the accepted floor or are malformed.
      /// </exception>
      void EnsureSufficientSlowHashParameters(KdfParameters parameters);

      /// <summary>
      /// Defaults for a new vault. Each access mints a fresh salt — capture once per database.
      /// </summary>
      KdfParameters DefaultSlowHashParameters { get; }

      int HashLength { get; }

      /// <summary>
      /// Onion AES-256-GCM: every passkey adds a layer; order matters.
      /// </summary>
      string EncryptSymmetrically(string source, string[] passwords);

      string DecryptSymmetrically(string source, string[] passwords);

      void GenerateRandomKeys(out string publicKey, out string privateKey);

      /// <summary>
      /// Hybrid encrypt: one-time AES for the payload, RSA-OAEP wraps that key (PEM).
      /// Used for activity records writable without being logged in.
      /// </summary>
      string EncryptAsymmetrically(string source, string key);

      string DecryptAsymmetrically(string source, string key);

      /// <summary>
      /// Public key for <paramref name="privateKey"/> — binds the activity log to the vault key pair.
      /// </summary>
      string GetPublicKey(string privateKey);

      /// <summary>RSA-PSS-SHA256 detached signature.</summary>
      string Sign(string source, string privateKey);

      /// <summary>
      /// Verifies a <see cref="Sign"/> signature. Returns <see langword="false"/> on
      /// tampering or malformed input instead of throwing.
      /// </summary>
      bool Verify(string source, string signature, string publicKey);
   }
}
