namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// KDF algorithm persisted in the vault header (crypto-agility).
   /// </summary>
   public enum KdfAlgorithm
   {
      Pbkdf2HmacSha256,
      Pbkdf2HmacSha512,
   }
}
