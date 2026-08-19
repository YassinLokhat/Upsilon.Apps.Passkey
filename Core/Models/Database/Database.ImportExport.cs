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
            eventType: ActivityEventType.ImportingDataStarted,
            data: [filePath],
            needsReview: true);

         string importContent = string.Empty;
         string errorLog = string.Empty;

         try
         {
            importContent = File.ReadAllText(filePath);
         }
#pragma warning disable CA1031 // Intentional: any file access failure is reported as a user-facing error message
         catch
#pragma warning restore CA1031
         {
            errorLog = $"import file is not accessible";
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ImportJson(importContent),
               ".csv" => this.ImportCSV(importContent),
               _ => $"'{extension}' extension type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataSucceded,
               data: [],
               needsReview: true);
            _save(logSaveEvent: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
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
            eventType: ActivityEventType.ExportingDataStarted,
            data: [filePath],
            needsReview: true);

         string errorLog = string.Empty;

         if (File.Exists(filePath))
         {
            errorLog = $"export file already exists";
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ExportJson(filePath),
               ".csv" => this.ExportCSV(filePath),
               _ => $"'{extension}' extension type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataSucceded,
               data: [],
               needsReview: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }
   }
}
