using System.Text.Json.Serialization;
using Upsilon.Apps.PassKey.Core.Internal.Models;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Internal.Utils
{
   internal class LogCenter
   {
      internal Database Database
      {
         get => field ?? throw new NullReferenceException(nameof(Database));
         set;
      }

      [JsonIgnore]
      public ILog[]? Logs => Database.User == null
               ? null
               : LogList.Select(x =>
               {
                  string textLog = Database.CryptographyCenter.DecryptAsymmetrically(x, Database.User.PrivateKey);
                  return Database.SerializationCenter.Deserialize<Log>(textLog);
               })
               .OrderByDescending(x => x.DateTime)
               .ToArray();

      public List<string> LogList { get; set; } = [];
      public string Username { get; set; } = string.Empty;
      public string PublicKey { get; set; } = string.Empty;

      public void AddLog(string message, bool needsReview)
      {
         Log log = new()
         {
            DateTime = DateTime.Now,
            Message = message,
            NeedsReview = needsReview,
         };

         string textLog = Database.SerializationCenter.Serialize(log);
         LogList.Add(Database.CryptographyCenter.EncryptAsymmetrically(textLog, PublicKey));

         _save();
      }

      private void _save()
      {
         if (Database.LogFileLocker == null) throw new NullReferenceException(nameof(Database.LogFileLocker));

         Database.LogFileLocker.Save(this, [Database.CryptographyCenter.GetHash(Username)]);
      }
   }
}
