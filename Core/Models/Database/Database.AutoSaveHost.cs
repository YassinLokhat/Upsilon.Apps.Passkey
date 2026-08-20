using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed partial class Database : IAutoSaveHost
   {
      ISerializationCenter IAutoSaveHost.SerializationCenter => SerializationCenter;

      void IAutoSaveHost.ResolveActivityNames(string itemId,
         ActivityEventType action,
         out string itemName,
         out string parentName)
      {
         itemName = string.Empty;
         parentName = string.Empty;

         if (itemId == User?.ItemId)
         {
            if (User is not null)
            {
               itemName = User.ToString();
            }
         }
         else if (itemId.StartsWith('S'))
         {
            Service? s = User?.Services.FirstOrDefault(x => x.ItemId == itemId);

            if (s is not null)
            {
               itemName = s.ToString();
            }
         }
         else if (itemId.StartsWith('A'))
         {
            Account? a = User?.Services.SelectMany(x => x.Accounts).FirstOrDefault(x => x.ItemId == itemId);

            if (a is not null)
            {
               itemName = a.ToString();

               if (action == ActivityEventType.ItemUpdated)
               {
                  parentName = a.Service.ToString();
               }
            }
         }
      }

      void IAutoSaveHost.AddActivity(string itemId,
         ActivityEventType eventType,
         string[] data,
         bool needsReview)
         => ActivityCenter.AddActivity(itemId, eventType, data, needsReview);

      void IAutoSaveHost.ApplyChange(Change change) => User?.Apply(change);

      bool IAutoSaveHost.AutoSaveEntryExists() => FileLocker.Exists(AutoSaveFileEntry);

      void IAutoSaveHost.DeleteAutoSaveEntry() => FileLocker.Delete(AutoSaveFileEntry);

      void IAutoSaveHost.SaveAutoSave(AutoSave autoSave)
         => FileLocker.Save(autoSave, AutoSaveFileEntry, Passkeys);
   }
}
