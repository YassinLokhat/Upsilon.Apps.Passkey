namespace Upsilon.Apps.PassKey.Core.Interfaces
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
      /// Returs a slow string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      string GetSlowHash(string source);

      /// <summary>
      /// The fixed length of the hash.
      /// </summary>
      int HashLength { get; }

      /// <summary>
      /// Sign a string.
      /// </summary>
      /// <param name="source">The string to sign. The method will modify the string to add the signature.</param>
      void Sign(ref string source);

      /// <summary>
      /// check the signature of a given string.
      /// </summary>
      /// <param name="source">The string to sign. The method will modify the string to remove the signature.</param>
      /// <returns>True if the signature is good, False else.</returns>
      bool CheckSign(ref string source);

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
