namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Kind of account identifier (login, contact channel, or auth method label).
   /// </summary>
   public enum IdentifierType
   {
      Username,
      Email,
      PhoneNumber,
      Passkey,
      AuthenticatorApp,
   }
}
