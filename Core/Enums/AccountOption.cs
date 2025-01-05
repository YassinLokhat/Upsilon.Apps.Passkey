namespace Upsilon.Apps.PassKey.Core.Enums
{
   [Flags]
   public enum AccountOption
   {
      None = 0b0000,
      RemindToUpdate = 0b0001,
      WarnIfPasswordLeaked = 0b0010,
   }
}