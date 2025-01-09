namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   /// <summary>
   /// Represent a cryptographic center.
   /// </summary>
   public interface ICryptographicCenter
   {
      /// <summary>
      /// Returs a fast string hash of the given string.
      /// </summary>
      /// <param name="source">The string to hash.</param>
      /// <returns>The hash.</returns>
      public string GetHash(string source);

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
      /// Encrypt a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to encrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The encrypted string.</returns>
      string Encrypt(string source, string[] passwords);

      /// <summary>
      /// Decrypt a string with a set of passekeys in an onion structure.
      /// </summary>
      /// <param name="source">The string to decrypt.</param>
      /// <param name="passwords">The set of passkeys.</param>
      /// <returns>The decrypted string.</returns>
      string Decrypt(string source, string[] passwords);
   }
}
