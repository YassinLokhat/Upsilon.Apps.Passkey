using System;
using System.Collections.Generic;
using System.Text;

namespace Upsilon.Apps.Passkey.Interfaces.Enums
{
   public enum ImportExportError
   {
      None = 0,
      ImportFileNotAccessible,
      ExtentionFileNotSupported,
      CSVHeadersDontMatch,
      IncorrectCSVFormat,
      NoDataToImport,
      ImportFileDeserializationFailed,
      ServiceAlreadyExists,
      BlankService,
      ExportFileAlreadyExists,
   }
}
