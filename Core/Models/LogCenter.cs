using System.Text.Json.Serialization;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Models
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
      public IEnumerable<ILog>? Logs
      {
         get
         {
            return Database.User == null
               ? null
               : (IEnumerable<ILog>)LogList.Select(x =>
            {
               string textLog = Database.CryptographicCenter.DecryptAsymmetrically(x, Database.User.PrivateKey);
               return Database.SerializationCenter.Deserialize<Log>(textLog);
            });
         }
      }

      public List<string> LogList { get; set; } = [];
      public string PublicKey { get; set; } = string.Empty;

      public void AddLog(string itemId, string message, bool needsReview)
      {
         Log log = new()
         {
            ItemId = itemId,
            Message = message,
            NeedsReview = needsReview,
         };

         string textLog = Database.SerializationCenter.Serialize(log);
         LogList.Add(Database.CryptographicCenter.EncryptAsymmetrically(textLog, PublicKey));

         _save();
      }

      private void _save()
      {
         if (Database.User == null) throw new NullReferenceException(nameof(Database.User));
         if (Database.LogFileLocker == null) throw new NullReferenceException(nameof(Database.LogFileLocker));

         Database.LogFileLocker.Save(this, [Database.CryptographicCenter.GetHash(Database.User.Username)]);
      }
   }
}
