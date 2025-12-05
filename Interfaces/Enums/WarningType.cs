namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Represent a type of warning.
   /// </summary>
   [Flags]
   public enum WarningType
   {
      /// <summary>
      /// A set of logs needs to be reviewed.
      /// </summary>
      LogReviewWarning = 0b0001,
      /// <summary>
      /// A set of accounts password expired and need to be updated.
      /// </summary>
      PasswordUpdateReminderWarning = 0b0010,
      /// <summary>
      /// Some accounts share the same passwords.
      /// </summary>
      DuplicatedPasswordsWarning = 0b0100,
      /// <summary>
      /// Some passwords leaked and found on the ';--have i been pwned? database
      /// </summary>
      PasswordLeakedWarning = 0b1000,
   }
}

