namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   /// <summary>
   /// Vault-level reasons behind a <see cref="Models.AlertKinds.VaultSecuritySettings"/> alert.
   /// Host issues live in <see cref="HostSecurityIssue"/>.
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
      /// <summary>No account has weak-password checks enabled (requires at least one account).</summary>
      NoAccountWeakPasswordCheck = 0b0000_0100_0000,
   }
}
