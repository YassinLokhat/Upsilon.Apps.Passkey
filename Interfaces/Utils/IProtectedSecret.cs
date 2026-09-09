namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// A secret held so its plaintext is only materialized just in time via
   /// <see cref="Reveal"/>. The concrete wrapping algorithm is an implementation
   /// detail of <see cref="ISecretMemoryProtector"/>.
   /// </summary>
   public interface IProtectedSecret
   {
      /// <summary>
      /// Decrypts or otherwise materializes the secret for a brief use. The
      /// returned string should be used and dropped promptly rather than stored.
      /// </summary>
      string Reveal();
   }
}
