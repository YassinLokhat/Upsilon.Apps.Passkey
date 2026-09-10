using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Concrete typed identifier used in vault JSON and API calls.
   /// </summary>
   public sealed class Identifier : IIdentifier, IEquatable<Identifier>
   {
      public Identifier()
      {
      }

      public Identifier(IdentifierType type, string value)
      {
         Type = type;
         Value = value ?? string.Empty;
      }

      public IdentifierType Type { get; set; }

      public string Value { get; set; } = string.Empty;

      public bool Equals(Identifier? other)
         => other is not null
            && Type == other.Type
            && string.Equals(Value, other.Value, StringComparison.Ordinal);

      public override bool Equals(object? obj)
         => Equals(obj as Identifier);

      public override int GetHashCode()
         => HashCode.Combine(Type, Value);

      public override string ToString() => Value;
   }
}
