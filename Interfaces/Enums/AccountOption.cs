namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Represent an account option.
   /// </summary>
   [Flags]
   public enum AccountOption
   {
      /// <summary>
      /// No option.
      /// </summary>
      None = 0b0000,
      /// <summary>
      /// Warn if the password leaked.
      /// </summary>
      WarnIfPasswordLeaked = 0b0010,
   }
}