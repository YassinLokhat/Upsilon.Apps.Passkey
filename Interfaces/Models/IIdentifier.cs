using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Typed login / contact identifier for an account.
   /// </summary>
   public interface IIdentifier
   {
      IdentifierType Type { get; }

      string Value { get; }
   }
}
