using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Core.Utils
{
   internal class ActivityCenter
   {
      internal Database Database
      {
         get => field ?? throw new NullReferenceException(nameof(Database));
         set;
      }

      internal List<IActivity> Activities = [];

      public List<string> ActivityList { get; set; } = [];

      public string Username { get; set; } = string.Empty;

      public string PublicKey { get; set; } = string.Empty;

      public void AddActivity(string itemId, ActivityEventType eventType, string[] data, bool needsReview)
      {
         Activity activity = new(DateTime.Now.Ticks, itemId, eventType, data, needsReview);

         Activities.Insert(0, activity);
         ActivityList.Insert(0, Database.CryptographyCenter.EncryptAsymmetrically(activity.ToString(), PublicKey));

         Save(rebuildStringActivities: false);
      }

      internal void LoadStringActivities()
      {
         Activities.Clear();

         if (Database.User is null) return;

         Activities = [.. ActivityList.AsParallel()
            .Select(x => new Activity(Database.CryptographyCenter.DecryptAsymmetrically(x, Database.User.PrivateKey)))
            .OrderByDescending(x => x.DateTime)];
      }

      public void Save(bool rebuildStringActivities)
      {
         if (rebuildStringActivities)
         {
            ActivityList.Clear();
            ActivityList.AddRange(Activities
               .OrderByDescending(x => x.DateTime)
               .Select(x => ((Activity)x).ToString())
               .Distinct()
               .Select(x => Database.CryptographyCenter.EncryptAsymmetrically(x, PublicKey)));
         }

         Database.FileLocker.Save(this, Database.ActivityFileEntry);
      }
   }
}
