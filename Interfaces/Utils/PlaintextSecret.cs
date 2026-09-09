namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Transient <see cref="IProtectedSecret"/> that holds plaintext. Used only
   /// when no <see cref="ISecretMemoryProtector"/> is available yet (e.g. import
   /// DTOs before they are attached to a vault host). Not for long-lived session
   /// storage — prefer <see cref="ISecretMemoryProtector.Protect"/>.
   /// </summary>
   public sealed class PlaintextSecret : IProtectedSecret
   {
      private readonly string _value;

      private PlaintextSecret(string value) => _value = value;

      public static IProtectedSecret Wrap(string? secret)
         => new PlaintextSecret(secret ?? string.Empty);

      public string Reveal() => _value;

      public override string ToString() => "***";
   }
}
