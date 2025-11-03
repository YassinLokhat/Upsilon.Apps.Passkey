using Upsilon.Apps.PassKey.Core.Internal.Utils;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Internal.Models
{
   internal sealed class AutoSave
   {
      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public Queue<Change> Changes { get; set; } = new();

      internal T UpdateValue<T>(string itemId, string itemName, string fieldName, bool needsReview, T oldValue, T value, string readableValue) where T : notnull
      {
         if (ISerializationCenter.AreDifferent(Database.SerializationCenter, oldValue, value))
         {
            _addChange(itemId, itemName, string.Empty, fieldName, Database.SerializationCenter.Serialize(value), readableValue, needsReview, Change.Type.Update);
         }

         return value;
      }

      internal T AddValue<T>(string itemId, string itemName, string containerName, bool needsReview, T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, Database.SerializationCenter.Serialize(value), string.Empty, needsReview, Change.Type.Add);

         return value;
      }

      internal T DeleteValue<T>(string itemId, string itemName, string containerName, bool needsReview, T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, Database.SerializationCenter.Serialize(value), string.Empty, needsReview, Change.Type.Delete);

         return value;
      }

      private void _addChange(string itemId, string itemName, string containerName, string fieldName, string value, string readableValue, bool needsReview, Change.Type action)
      {
         Queue<Change> changes = new();

         while (Changes.Count != 0)
         {
            Change change = Changes.Dequeue();
            
            if (change.ItemId != itemId
               || change.FieldName != fieldName
               || change.ActionType != action)
            {
               changes.Enqueue(change);
            }
         }

         changes.Enqueue(new Change
         {
            ActionType = action,
            ItemId = itemId,
            FieldName = fieldName,
            Value = value,
         });

         Changes = changes;

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

      internal void MergeChange()
      {
         while (Changes.Count != 0)
         {
            Database.User?.Apply(Changes.Dequeue());
         }

         Clear();
      }

      internal void Clear()
      {
         Changes.Clear();

         Database.AutoSaveFileLocker?.Dispose();
         Database.AutoSaveFileLocker = null;

         if (File.Exists(Database.AutoSaveFile))
         {
            File.Delete(Database.AutoSaveFile);
         }
      }
   }
}