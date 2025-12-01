using Upsilon.Apps.Passkey.Core.Internal.Utils;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Internal.Models
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
         string itemName,
         string fieldName,
         bool needsReview,
         T oldValue,
         T newValue,
         string readableValue) where T : notnull
      {
         if (ISerializationCenter.AreDifferent(Database.SerializationCenter, oldValue, newValue))
         {
            _addChange(itemId,
               itemName,
               string.Empty,
               fieldName,
               oldValue.SerializeWith(Database.SerializationCenter),
               newValue.SerializeWith(Database.SerializationCenter),
               readableValue,
               needsReview,
               Change.Type.Update);
         }

         return newValue;
      }

      internal T AddValue<T>(string itemId,
         string itemName,
         string containerName,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, value.SerializeWith(Database.SerializationCenter), string.Empty, needsReview, Change.Type.Add);

         return value;
      }

      internal T DeleteValue<T>(string itemId,
         string itemName,
         string containerName,
         bool needsReview,
         T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, value.SerializeWith(Database.SerializationCenter), string.Empty, needsReview, Change.Type.Delete);

         return value;
      }

      private void _addChange(string itemId,
         string itemName,
         string containerName,
         string fieldName,
         string newValue,
         string readableValue,
         bool needsReview,
         Change.Type action)
      {
         _addChange(itemId,
            itemName,
            containerName,
            fieldName,
            null,
            newValue,
            readableValue,
            needsReview,
            action);
      }

      private void _addChange(string itemId,
         string itemName,
         string containerName,
         string fieldName,
         string? oldValue,
         string newValue,
         string readableValue,
         bool needsReview,
         Change.Type action)
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

         if (Database.AutoSaveFileLocker == null)
         {
            Database.AutoSaveFileLocker = new(Database.CryptographyCenter, Database.SerializationCenter, Database.AutoSaveFile, FileMode.OpenOrCreate);
         }

         Database.AutoSaveFileLocker.Save(this, Database.Passkeys);
         string logMessage = action switch
         {
            Change.Type.Add => $"{itemName} has been added to {containerName}",
            Change.Type.Delete => $"{itemName} has been removed from {containerName}",
            _ => $"{itemName}'s {fieldName.ToSentenceCase().ToLower()} has been {(string.IsNullOrWhiteSpace(readableValue) ? $"updated" : $"set to {readableValue}")}",
         };
         Database.Logs.AddLog(logMessage, needsReview);
      }

      private void _mergeChanges(string changeKey, Change currentChange)
      {
         Change? lastUpdate = Changes[changeKey].LastOrDefault(x => x.ActionType == Change.Type.Update);

         if (currentChange.ActionType != Change.Type.Update
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
         List<Change> changes = Changes.Values.SelectMany(x => x).OrderBy(x => x.Index).ToList();

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

         Database.AutoSaveFileLocker?.Dispose();
         Database.AutoSaveFileLocker = null;

         if (deleteFile
            && File.Exists(Database.AutoSaveFile))
         {
            File.Delete(Database.AutoSaveFile);
         }
      }
   }
}