namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   [Flags]
   public enum WarningType
   {
      ActivityReviewWarning = 0b00001,
      PasswordUpdateReminderWarning = 0b00010,
      DuplicatedPasswordsWarning = 0b00100,
      PasswordLeakedWarning = 0b01000,
      /// <summary>User or account settings that weaken session / monitoring posture.</summary>
      SecuritySettingsWarning = 0b10000,
   }
}
