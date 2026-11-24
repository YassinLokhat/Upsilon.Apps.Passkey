using System.Security;
using System.Text;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database
   {
      public ImportExportError ImportFromFile(string filePath)
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
            fieldValue: Path.GetFileName(filePath),
            parentName: null,
            eventType: ActivityEventType.ImportingDataStarted,
            needsReview: true);

         string importContent = string.Empty;
         ImportExportError errorLog = ImportExportError.None;

         try
         {
            FileInfo importInfo = new(filePath);
            if (!importInfo.Exists)
            {
               errorLog = ImportExportError.ImportFileNotAccessible;
            }
            else if (importInfo.Length > ResourceBudgets.MaxImportFileBytes)
            {
               errorLog = ImportExportError.ImportFileTooLarge;
            }
            else
            {
               importContent = File.ReadAllText(filePath);
            }
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
               _ => ImportExportError.ExtensionFileNotSupported,
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
               eventType: ActivityEventType.ImportingDataSucceeded,
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

         return errorLog;
      }

      public ImportExportError ExportToFile(string filePath)
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
            fieldValue: Path.GetFileName(filePath),
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
               _ => ImportExportError.ExtensionFileNotSupported,
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
               eventType: ActivityEventType.ExportingDataSucceeded,
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

         return errorLog;
      }

      public ImportExportError ImportFromStream(Stream stream, string formatExtension)
      {
         ArgumentNullException.ThrowIfNull(stream);
         ArgumentException.ThrowIfNullOrWhiteSpace(formatExtension);

         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         string extension = _normalizeExtension(formatExtension);

         ActivityCenter.AddActivity(itemId: string.Empty,
            Username,
            serviceName: null,
            accountName: null,
            fieldName: nameof(formatExtension),
            fieldValue: extension,
            parentName: null,
            eventType: ActivityEventType.ImportingDataStarted,
            needsReview: true);

         ImportExportError errorLog = ImportExportError.None;
         string importContent = string.Empty;

         try
         {
            if (stream.CanSeek
                && stream.Length > ResourceBudgets.MaxImportFileBytes)
            {
               errorLog = ImportExportError.ImportFileTooLarge;
            }
            else
            {
               using StreamReader reader = new(stream, leaveOpen: true);
               string? bounded = _readBounded(reader);
               importContent = bounded ?? string.Empty;
               if (bounded is null
                   || Encoding.UTF8.GetByteCount(importContent) > ResourceBudgets.MaxImportFileBytes)
               {
                  errorLog = ImportExportError.ImportFileTooLarge;
                  importContent = string.Empty;
               }
            }
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ObjectDisposedException)
         {
            errorLog = ImportExportError.ImportFileNotAccessible;
         }

         if (errorLog == ImportExportError.None)
         {
            errorLog = extension switch
            {
               ".JSON" => this.ImportJson(importContent),
               ".CSV" => this.ImportCSV(importContent),
               _ => ImportExportError.ExtensionFileNotSupported,
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
               eventType: ActivityEventType.ImportingDataSucceeded,
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

         return errorLog;
      }

      public ImportExportError ExportToStream(Stream stream, string formatExtension)
      {
         ArgumentNullException.ThrowIfNull(stream);
         ArgumentException.ThrowIfNullOrWhiteSpace(formatExtension);

         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         string extension = _normalizeExtension(formatExtension);

         ActivityCenter.AddActivity(itemId: string.Empty,
            Username,
            serviceName: null,
            accountName: null,
            fieldName: nameof(formatExtension),
            fieldValue: extension,
            parentName: null,
            eventType: ActivityEventType.ExportingDataStarted,
            needsReview: true);

         ImportExportError errorLog = ImportExportError.None;

         try
         {
            string? content = extension switch
            {
               ".JSON" => this.BuildExportJson(),
               ".CSV" => this.BuildExportCsv(),
               _ => null,
            };

            if (content is null)
            {
               errorLog = ImportExportError.ExtensionFileNotSupported;
            }
            else
            {
               using StreamWriter writer = new(stream, leaveOpen: true);
               writer.Write(content);
               writer.Flush();
            }
         }
         catch (Exception ex)
            when (ex is ArgumentException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ObjectDisposedException)
         {
            errorLog = ImportExportError.ImportFileNotAccessible;
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
               eventType: ActivityEventType.ExportingDataSucceeded,
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

         return errorLog;
      }

      /// <summary>Returns <see langword="null"/> once the text exceeds the import budget.</summary>
      private static string? _readBounded(StreamReader reader)
      {
         // UTF-8 never encodes a char in fewer than one byte, so the char count bounds the byte count from below.
         StringBuilder content = new();
         char[] buffer = new char[81920];
         int read;
         while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
         {
            if (content.Length + read > ResourceBudgets.MaxImportFileBytes)
            {
               return null;
            }

            _ = content.Append(buffer, 0, read);
         }

         return content.ToString();
      }

      private static string _normalizeExtension(string formatExtension)
      {
         string extension = formatExtension.StartsWith('.')
            ? formatExtension
            : "." + formatExtension;
         return extension.ToUpperInvariant();
      }
   }
}
