namespace Upsilon.Apps.Passkey.Core.Models
{
   [Flags]
   public enum AccountOption
   {
      None = 0b0000,
      RemindToUpdate = 0b0001,
      WarnIfPasswordLeaked = 0b0010,
   }
}