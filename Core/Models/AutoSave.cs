using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class AutoSave
   {
      internal Database Database
      {
         get => field ?? throw new NullReferenceException(nameof(Database));
         set;
      }

      public Dictionary<string, List<Change>> Changes { get; set; } = [];

      internal T UpdateValue<T>(string itemId,
         string fieldName,
         bool needsReview,
         T oldValue,
         T newValue,
         string readableValue) where T : notnull
      {
         if (Database.SerializationCenter.AreDifferent(oldValue, newValue))
         {
            _addChange(itemId,
               fieldName,
               oldValue.SerializeWith(Database.SerializationCenter),
               newValue.SerializeWith(Database.SerializationCenter),
               readableValue,
               needsReview,
               ActivityEventType.ItemUpdated);
         }

         return newValue;
      }

      internal T AddValue<T>(string itemId,
         string readableValue,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.SerializeWith(Database.SerializationCenter), readableValue, needsReview, ActivityEventType.ItemAdded);

         return value;
      }

      internal T DeleteValue<T>(string itemId,
         string readableValue,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.SerializeWith(Database.SerializationCenter), readableValue, needsReview, ActivityEventType.ItemDeleted);

         return value;
      }

      private void _addChange(string itemId,
         string fieldName,
         string newValue,
         string readableValue,
         bool needsReview,
         ActivityEventType action)
      {
         _addChange(itemId,
            fieldName,
            null,
            newValue,
            readableValue,
            needsReview,
            action);
      }

      private void _addChange(string itemId,
         string fieldName,
         string? oldValue,
         string newValue,
         string readableValue,
         bool needsReview,
         ActivityEventType action)
      {
         string changeKey = $"{itemId}\t{fieldName}";
         if (!Changes.ContainsKey(changeKey))
         {
            Changes[changeKey] = [];
         }

         Change currentChange = new()
         {
            Index = DateTime.Now.Ticks,
            ActionType = action,
            ItemId = itemId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
         };

         _mergeChanges(changeKey, currentChange);

         Database.FileLocker.Save(this, Database.AutoSaveFileEntry, Database.Passkeys);
         string itemName = string.Empty;

         if (itemId == Database.User?.ItemId)
         {
            if (Database.User is not null)
            {
               itemName = Database.User.ToString();
            }
         }
         else if (itemId.StartsWith('S'))
         {
            Service? s = Database.User?.Services.FirstOrDefault(x => x.ItemId == itemId);

            if (s is not null)
            {
               itemName = s.ToString();
            }
         }
         else if (itemId.StartsWith('A'))
         {
            Account? a = Database.User?.Services.SelectMany(x => x.Accounts).FirstOrDefault(x => x.ItemId == itemId);

            if (a is not null)
            {
               itemName = a.ToString();
            }
         }

         Database.ActivityCenter.AddActiivity(itemId: itemId,
            eventType: action,
            data: [itemName, fieldName, readableValue],
            needsReview);
      }

      private void _mergeChanges(string changeKey, Change currentChange)
      {
         Change? lastUpdate = Changes[changeKey].LastOrDefault(x => x.ActionType == ActivityEventType.ItemUpdated);

         if (currentChange.ActionType != ActivityEventType.ItemUpdated
            || lastUpdate is null)
         {
            Changes[changeKey].Add(currentChange);
            return;
         }

         _ = Changes[changeKey].Remove(lastUpdate);
         currentChange.OldValue = lastUpdate.OldValue;

         if (currentChange.OldValue != currentChange.NewValue)
         {
            Changes[changeKey].Add(currentChange);
         }
         else if (Changes[changeKey].Count == 0)
         {
            _ = Changes.Remove(changeKey);
         }
      }

      internal void ApplyChanges(bool deleteFile)
      {
         List<Change> changes = [.. Changes.Values.SelectMany(x => x).OrderBy(x => x.Index)];

         foreach (Change change in changes)
         {
            Database.User?.Apply(change);
         }

         if (deleteFile)
         {
            Clear(deleteFile: true);
         }
      }

      internal bool Any() => Any(string.Empty);

      internal bool Any(string itemId) => Changes.Any(x => x.Key.StartsWith(itemId));

      internal bool Any(string itemId, string fieldName) => Changes.Any(x => x.Key == $"{itemId}\t{fieldName}");

      internal void Clear(bool deleteFile)
      {
         Changes.Clear();

         if (deleteFile
            && Database.FileLocker.Exists(Database.AutoSaveFileEntry))
         {
            Database.FileLocker.Delete(Database.AutoSaveFileEntry);
         }
      }
   }
}