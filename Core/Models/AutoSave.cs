using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.PassKey.Core.Models
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

      internal T UpdateValue<T>(string itemId, string fieldName, T value) where T : notnull
      {
         _addChange(itemId, fieldName, Database.SerializationCenter.Serialize(value), ChangeType.Update);

         return value;
      }

      internal T AddValue<T>(string itemId, T value) where T : notnull
      {
         _addChange(itemId, string.Empty, Database.SerializationCenter.Serialize(value), ChangeType.Add);

         return value;
      }

      internal T DeleteValue<T>(string itemId, T value) where T : notnull
      {
         _addChange(itemId, string.Empty, Database.SerializationCenter.Serialize(value), ChangeType.Delete);

         return value;
      }

      private void _addChange(string itemId, string fieldName, string value, ChangeType action)
      {
         Changes.Enqueue(new Change
         {
            ActionType = action,
            ItemId = itemId,
            FieldName = fieldName,
            Value = value,
         });

         if (Database.AutoSaveFileLocker == null)
         {
            Database.AutoSaveFileLocker = new(Database.CryptographicCenter, Database.SerializationCenter, Database.AutoSaveFile, FileMode.OpenOrCreate);
         }

         Database.AutoSaveFileLocker.WriteAllText(Database.SerializationCenter.Serialize(this), Database.Passkeys);
      }

      internal void MergeChange()
      {
         while (Changes.Count != 0)
         {
            Database.User?.Apply(Changes.Dequeue());
         }

         Database.Save();

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

         // LOG "Autosave cleared"
      }
   }
}