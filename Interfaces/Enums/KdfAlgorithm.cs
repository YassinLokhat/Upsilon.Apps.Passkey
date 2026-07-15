namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// The key-derivation function used to stretch a passkey into key material.
   /// Persisted in the database header so the correct algorithm can be selected
   /// when the file is reopened, even after the default evolves.
   /// </summary>
   public enum KdfAlgorithm
   {
      /// <summary>
      /// PBKDF2 with an HMAC-SHA-256 pseudo-random function.
      /// </summary>
      Pbkdf2HmacSha256,

      /// <summary>
      /// PBKDF2 with an HMAC-SHA-512 pseudo-random function.
      /// </summary>
      Pbkdf2HmacSha512,
   }
}
