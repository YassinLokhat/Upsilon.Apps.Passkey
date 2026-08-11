namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Represent a cryptographic center.
   /// </summary>
   public interface ICryptographyCenter
   {
      /// <summary>
      /// Returs a fast string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      string GetHash(string source);

      /// <summary>
      /// Returs a slow string hash of the given string, using the provided key-derivation parameters.
      /// This enables crypto-agility: a file is always reopened with the exact parameters it was
      /// written with (algorithm, iterations, output length and the random salt), which are stored
      /// in its header. Parameters are checked against
      /// <see cref="EnsureSufficientSlowHashParameters"/> before stretching.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <param name="parameters">The key-derivation parameters (algorithm, iterations, output length and salt) to use.</param>
      /// <returns>The hash.</returns>
      /// <exception cref="InsufficientKdfParametersException">
      /// The parameters fall below the accepted floor or are malformed.
      /// </exception>
      string GetSlowHash(string source, KdfParameters parameters);

      /// <summary>
      /// Ensures the given stretching parameters meet the minimum floor accepted by
      /// this crypto center (iterations, output length, salt size, scheme version,
      /// and a known algorithm). Called when a database header is read and again
      /// before every slow hash, so a weakened or malformed header is rejected
      /// instead of being used to stretch passkeys.
      /// </summary>
      /// <param name="parameters">The key-derivation parameters to check.</param>
      /// <exception cref="InsufficientKdfParametersException">
      /// The parameters fall below the accepted floor or are malformed.
      /// </exception>
      void EnsureSufficientSlowHashParameters(KdfParameters parameters);

      /// <summary>
      /// The key-derivation parameters used to stretch passkeys for newly created databases.
      /// Each access mints a fresh random salt, so the returned instance must be captured once
      /// per database and then recorded in its header, so the file remains readable if these
      /// values change in a future release. Defaults always satisfy
      /// <see cref="EnsureSufficientSlowHashParameters"/>.
      /// </summary>
      KdfParameters DefaultSlowHashParameters { get; }

      /// <summary>
      /// The fixed length of the hash.
      /// </summary>
      int HashLength { get; }

      /// <summary>
      /// Encrypt symmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The encrypted string.</returns>
      string EncryptSymmetrically(string source, string[] passwords);

      /// <summary>
      /// Decrypt symmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The decrypted string.</returns>
      string DecryptSymmetrically(string source, string[] passwords);

      /// <summary>
      /// Generate a random public key and private key pair.
      /// </summary>
      /// <param name="publicKey">The public key generated.</param>
      /// <param name="privateKey">The private key generated.</param>
      void GenerateRandomKeys(out string publicKey, out string privateKey);

      /// <summary>
      /// Encrypt asymmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="key">The encryption key.</param>
      /// <returns>The encrypted string.</returns>
      string EncryptAsymmetrically(string source, string key);

      /// <summary>
      /// Decrypt asymmetrically a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="key">The encryption key.</param>
      /// <returns>The decrypted string.</returns>
      string DecryptAsymmetrically(string source, string key);

      /// <summary>
      /// Derives the public key that corresponds to the given private key.
      /// Used to bind data (e.g. the activity log) to the key pair stored in the
      /// encrypted database, so an attacker cannot substitute their own key.
      /// </summary>
      /// <param name="privateKey">The private key.</param>
      /// <returns>The matching public key.</returns>
      string GetPublicKey(string privateKey);

      /// <summary>
      /// Signs a string with a private key, producing a detached signature that
      /// can later be checked with <see cref="Verify"/> and the matching public key.
      /// </summary>
      /// <param name="source">The string to sign.</param>
      /// <param name="privateKey">The private key used to sign.</param>
      /// <returns>The signature.</returns>
      string Sign(string source, string privateKey);

      /// <summary>
      /// Verifies that a signature produced by <see cref="Sign"/> matches the given
      /// string and public key. Returns <see langword="false"/> for any tampering
      /// or malformed input instead of throwing.
      /// </summary>
      /// <param name="source">The signed string.</param>
      /// <param name="signature">The signature to check.</param>
      /// <param name="publicKey">The public key matching the signing private key.</param>
      /// <returns><see langword="true"/> if the signature is valid; otherwise <see langword="false"/>.</returns>
      bool Verify(string source, string signature, string publicKey);
   }
}
