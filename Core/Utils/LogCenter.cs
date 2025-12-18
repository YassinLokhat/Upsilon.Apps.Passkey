using System.Text.Json.Serialization;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal class LogCenter
   {
      internal Database Database
      {
         get => field ?? throw new NullReferenceException(nameof(Database));
         set;
      }

      internal List<ILog> Logs = [];

      public List<string> LogList { get; set; } = [];

      public string Username { get; set; } = string.Empty;

      public string PublicKey { get; set; } = string.Empty;

      public void AddLog(string[] data, LogEventType eventType, bool needsReview)
      {
         Log log = new()
         {
            DateTimeTicks = DateTime.Now.Ticks,
            Data = data,
            EventType = eventType,
            NeedsReview = needsReview,
         };

         Logs.Add(log);
         LogList.Add(Database.CryptographyCenter.EncryptAsymmetrically(log.ToString(), PublicKey));

         _save();
      }

      internal void LoadStringLogs()
      {
         Logs.Clear();

         if (Database.User is null) return;

         foreach (string log in LogList)
         {
            Logs.Add(new Log(Database, Database.CryptographyCenter.DecryptAsymmetrically(log, Database.User.PrivateKey)));
         }
      }

      private void _save()
      {
         Database.FileLocker.Save(this, Database.LogFileEntry);
      }
   }
}
