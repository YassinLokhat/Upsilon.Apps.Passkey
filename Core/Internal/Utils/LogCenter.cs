using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Core.Internal.Models;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Internal.Utils
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
               : LogList.Select(x => Database.CryptographyCenter
                     .DecryptAsymmetrically(x, Database.User.PrivateKey)
                     .DeserializeTo<Log>(Database.SerializationCenter))
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

         string textLog = log.SerializeWith(Database.SerializationCenter);
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
