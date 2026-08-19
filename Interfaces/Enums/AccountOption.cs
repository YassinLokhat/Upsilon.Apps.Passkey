namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   [Flags]
   public enum AccountOption
   {
      None = 0b0000,
      WarnIfPasswordLeaked = 0b0001,
      WarnIfDuplicatedPassword = 0b0010,
   }
}
