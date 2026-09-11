namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   public enum ImportExportError
   {
      None = 0,
      ImportFileNotAccessible,
      ExtensionFileNotSupported,
      CSVHeadersDontMatch,
      IncorrectCSVFormat,
      NoDataToImport,
      ImportFileDeserializationFailed,
      ServiceAlreadyExists,
      BlankService,
      ExportFileAlreadyExists,
   }
}
