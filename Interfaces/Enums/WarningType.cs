namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   [Flags]
   public enum WarningType
   {
      ActivityReviewWarning = 0b0001,
      PasswordUpdateReminderWarning = 0b0010,
      DuplicatedPasswordsWarning = 0b0100,
      PasswordLeakedWarning = 0b1000,
   }
}
