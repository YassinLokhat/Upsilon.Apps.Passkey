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

      public void AddLog(string itemId, LogEventType eventType, string[] data, bool needsReview)
      {
         Log log = new(DateTime.Now.Ticks, itemId, eventType, data, needsReview);

         Logs.Insert(0, log);
         LogList.Insert(0, Database.CryptographyCenter.EncryptAsymmetrically(log.ToString(), PublicKey));

         Save(rebuildStringLogs: false);
      }

      internal void LoadStringLogs()
      {
         Logs.Clear();

         if (Database.User is null) return;

         foreach (string log in LogList)
         {
            Logs.Add(new Log(Database.CryptographyCenter.DecryptAsymmetrically(log, Database.User.PrivateKey)));
         }

         Logs = [.. Logs.OrderByDescending(x => x.DateTime)];
      }

      public void Save(bool rebuildStringLogs)
      {
         if (rebuildStringLogs)
         {
            LogList.Clear();
            LogList.AddRange(Logs
               .OrderByDescending(x => x.DateTime)
               .Select(x => ((Log)x).ToString())
               .Distinct()
               .Select(x => Database.CryptographyCenter.EncryptAsymmetrically(x, PublicKey)));
         }

         Database.FileLocker.Save(this, Database.LogFileEntry);
      }
   }
}
