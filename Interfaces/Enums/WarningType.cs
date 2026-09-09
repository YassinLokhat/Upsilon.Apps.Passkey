namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Legacy warning-type flags used only to migrate older vault
   /// <c>AlertsToNotify</c> / <c>WarningsToNotify</c> values. Prefer <see cref="Models.AlertKinds"/>.
   /// </summary>
   [Flags]
   [Obsolete("Use AlertKinds string identifiers instead.")]
   public enum WarningType
   {
      ActivityReviewWarning = 0b00001,
      PasswordUpdateReminderWarning = 0b00010,
      DuplicatedPasswordsWarning = 0b00100,
      PasswordLeakedWarning = 0b01000,
      SecuritySettingsWarning = 0b10000,
   }
}
