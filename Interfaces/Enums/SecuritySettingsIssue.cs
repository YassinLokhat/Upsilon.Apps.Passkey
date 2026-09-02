namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Concrete reasons behind a <see cref="WarningType.SecuritySettingsWarning"/>.
   /// </summary>
   [Flags]
   public enum SecuritySettingsIssue
   {
      None = 0,
      AutoLogoutDisabled = 0b0000_0000_0001,
      ClipboardCleaningDisabled = 0b0000_0000_0010,
      QrAutoCloseDisabled = 0b0000_0000_0100,
      /// <summary>No account has leak checks enabled (requires at least one account).</summary>
      NoAccountLeakCheck = 0b0000_0000_1000,
      /// <summary>No account has duplicate checks enabled (requires at least one account).</summary>
      NoAccountDuplicateCheck = 0b0000_0001_0000,
      /// <summary>No account has a password-update reminder delay (requires at least one account).</summary>
      NoAccountUpdateReminder = 0b0000_0010_0000,
      /// <summary>App-level login-window idle clear is disabled (<c>LoginIdleTimeoutSeconds == 0</c>).</summary>
      IdleLoginDisabled = 0b0000_0100_0000,
      /// <summary>No offline leak Bloom filter is currently loaded for fail-over.</summary>
      OfflineLeakFilterUnavailable = 0b0000_1000_0000,
      /// <summary>
      /// <see cref="WarningType.DuplicatedPasswordsWarning"/> is not included in
      /// <c>WarningsToNotify</c> (User Settings).
      /// </summary>
      DuplicatePasswordNotificationsDisabled = 0b0001_0000_0000,
      /// <summary>
      /// <see cref="WarningType.PasswordUpdateReminderWarning"/> is not included in
      /// <c>WarningsToNotify</c> (User Settings).
      /// </summary>
      PasswordUpdateReminderNotificationsDisabled = 0b0010_0000_0000,
      /// <summary>
      /// <see cref="WarningType.PasswordLeakedWarning"/> is not included in
      /// <c>WarningsToNotify</c> (User Settings).
      /// </summary>
      PasswordLeakedNotificationsDisabled = 0b0100_0000_0000,
   }
}
