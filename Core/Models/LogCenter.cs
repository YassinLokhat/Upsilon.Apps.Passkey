namespace Upsilon.Apps.PassKey.Core.Models
{
   internal class LogCenter
   {
      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public List<Log> Logs { get; set; } = [];
      public string PublicKey { get; set; } = string.Empty;

      public void AddLog(string itemId, string message)
      {
         Logs.Add(new()
         {
            ItemId = itemId,
            Message = message,
         });

         _save();
      }

      private void _save()
      {
         if (Database.User == null) throw new NullReferenceException(nameof(Database.User));
         if (Database.LogFileLocker == null) throw new NullReferenceException(nameof(Database.LogFileLocker));

         Database.LogFileLocker.Save(this, PublicKey);
      }
   }
}
