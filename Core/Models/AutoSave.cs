using System.Text.Json.Serialization;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class AutoSave
   {
      private Database? _database;
      [JsonIgnore]
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public Queue<Change> Changes { get; set; } = new();

      public T UpdateValue<T>(string itemId, string fieldName, T value) where T : notnull
      {
         _addChange(itemId, fieldName, value.Serialize(), ChangeType.Update);

         return value;
      }

      public T AddValue<T>(string itemId, T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.Serialize(), ChangeType.Add);

         return value;
      }

      public T DeleteValue<T>(string itemId, T value) where T : notnull
      {
         _addChange(itemId, string.Empty, value.Serialize(), ChangeType.Delete);

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
            Database.AutoSaveFileLocker = new(Database.AutoSaveFile, FileMode.OpenOrCreate);
         }

         Database.AutoSaveFileLocker.WriteAllText(this.Serialize(), Database.Passkeys);
      }

      public void MergeChange()
      {
         while (Changes.Any())
         {
            Database.User?.Apply(Changes.Dequeue());
         }

         Database.Save();

         Clear();
      }

      public void Clear()
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