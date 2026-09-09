namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Wraps secrets for in-memory holding. Injected at vault Create/Open so Core
   /// never depends on a concrete algorithm (AES, Caesar, …).
   /// </summary>
   public interface ISecretMemoryProtector
   {
      /// <summary>
      /// Encrypts or otherwise wraps a secret so it can be held without keeping
      /// its plaintext in a long-lived field.
      /// </summary>
      IProtectedSecret Protect(string? secret);
   }
}
