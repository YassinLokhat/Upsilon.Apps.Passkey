using System.Security;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      public bool ImportFromFile(string filePath)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         ActivityCenter.AddActivity(itemId: string.Empty,
            Username,
            serviceName: null,
            accountName: null,
            fieldName: nameof(filePath),
            fieldValue: filePath,
            parentName: null,
            eventType: ActivityEventType.ImportingDataStarted,
            needsReview: true);

         string importContent = string.Empty;
         ImportExportError errorLog = ImportExportError.None;

         try
         {
            importContent = File.ReadAllText(filePath);
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or ArgumentNullException
            or PathTooLongException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or SecurityException)
         {
            errorLog = ImportExportError.ImportFileNotAccessible;
         }

         if (errorLog == ImportExportError.None)
         {
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ImportJson(importContent),
               ".csv" => this.ImportCSV(importContent),
               _ => ImportExportError.ExtentionFileNotSupported,
            };
         }

         if (errorLog == ImportExportError.None)
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               Username,
               serviceName: null,
               accountName: null,
               fieldName: null,
               fieldValue: null,
               parentName: null,
               eventType: ActivityEventType.ImportingDataSucceded,
               needsReview: true);
            _save(logSaveEvent: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               Username,
               serviceName: null,
               accountName: null,
               fieldName: nameof(ImportExportError),
               fieldValue: $"{errorLog}",
               parentName: null,
               eventType: ActivityEventType.ImportingDataFailed,
               needsReview: true);
         }

         return errorLog == ImportExportError.None;
      }

      public bool ExportToFile(string filePath)
      {
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         ActivityCenter.AddActivity(itemId: string.Empty,
            Username,
            serviceName: null,
            accountName: null,
            fieldName: nameof(filePath),
            fieldValue: filePath,
            parentName: null,
            eventType: ActivityEventType.ExportingDataStarted,
            needsReview: true);

         ImportExportError errorLog = ImportExportError.None;

         if (File.Exists(filePath))
         {
            errorLog = ImportExportError.ExportFileAlreadyExists;
         }

         if (errorLog == ImportExportError.None)
         {
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ExportJson(filePath),
               ".csv" => this.ExportCSV(filePath),
               _ => ImportExportError.ExtentionFileNotSupported,
            };
         }

         if (errorLog == ImportExportError.None)
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               Username,
               serviceName: null,
               accountName: null,
               fieldName: null,
               fieldValue: null,
               parentName: null,
               eventType: ActivityEventType.ExportingDataSucceded,
               needsReview: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               Username,
               serviceName: null,
               accountName: null,
               fieldName: nameof(ImportExportError),
               fieldValue: $"{errorLog}",
               parentName: null,
               eventType: ActivityEventType.ExportingDataFailed,
               needsReview: true);
         }

         return errorLog == ImportExportError.None;
      }
   }
}
