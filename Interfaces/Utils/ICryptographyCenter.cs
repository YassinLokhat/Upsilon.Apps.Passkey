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
      /// Returs a slow string hash of the given string, using <see cref="DefaultSlowHashParameters"/>.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <param name="salt">A stable, per-account value (typically the username) mixed into the
      /// salt so that identical <paramref name="source"/> values hash differently across accounts.</param>
      /// <returns>The hash.</returns>
      string GetSlowHash(string source, string salt);

      /// <summary>
      /// Returs a slow string hash of the given string, using the provided key-derivation parameters.
      /// This overload enables crypto-agility: a file is always reopened with the exact parameters it
      /// was written with, which are stored in its header.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <param name="salt">A stable, per-account value (typically the username) mixed into the
      /// salt so that identical <paramref name="source"/> values hash differently across accounts.</param>
      /// <param name="parameters">The key-derivation parameters (algorithm, iterations, output length) to use.</param>
      /// <returns>The hash.</returns>
      string GetSlowHash(string source, string salt, KdfParameters parameters);

      /// <summary>
      /// The key-derivation parameters used to stretch passkeys for newly created databases.
      /// They are recorded in each database header so the file remains readable if these values
      /// change in a future release.
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
   }
}
