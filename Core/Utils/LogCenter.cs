using System.Text.Json.Serialization;
using Upsilon.Apps.PassKey.Core.Models;
using Upsilon.Apps.PassKey.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Utils
{
   internal class LogCenter
   {
      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      [JsonIgnore]
      public ILog[]? Logs
      {
         get
         {
            return Database.User == null
               ? null
               : LogList.Select(x =>
               {
                  string textLog = Database.CryptographyCenter.DecryptAsymmetrically(x, Database.User.PrivateKey);
                  return Database.SerializationCenter.Deserialize<Log>(textLog);
               })
               .OrderByDescending(x => x.DateTime)
               .ToArray();
         }
      }

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
