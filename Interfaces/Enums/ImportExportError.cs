namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   public enum ImportExportError
   {
      None = 0,
      ImportFileNotAccessible,
      ImportFileTooLarge,
      ExtensionFileNotSupported,
      CSVHeadersDontMatch,
      IncorrectCSVFormat,
      NoDataToImport,
      ImportFileDeserializationFailed,
      ServiceAlreadyExists,
      BlankService,
      ExportFileAlreadyExists,
      /// <summary>
      /// JSON settings would set LogoutTimeout, CleaningClipboardTimeout, or
      /// ShowPasswordDelay to 0, disabling session exposure controls.
      /// </summary>
      SecurityTimeoutsDisabled,
   }
}
