using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed class Database : IDatabase
   {
      #region IUser interface explicit implementation

      public string DatabaseFile { get; set; }
      public string AutoSaveFile { get; set; }
      public string LogFile { get; set; }

      IUser? IDatabase.User { get => User; }

      public void Delete()
      {
         DatabaseFileLocker?.Delete();

         if (File.Exists(AutoSaveFile))
         {
            AutoSaveFileLocker?.Delete();
         }

         Dispose();
      }

      public void Dispose()
      {
         User = null;
         AutoSave.Changes.Clear();
         Passkeys = Array.Empty<string>();

         DatabaseFileLocker?.Dispose();
         DatabaseFileLocker = null;

         AutoSaveFileLocker?.Dispose();
         AutoSaveFileLocker = null;

         DatabaseFile = string.Empty;
         AutoSaveFile = string.Empty;
         LogFile = string.Empty;
      }

      public void HandleAutoSave(bool mergeAutoSave)
      {
         if (!File.Exists(AutoSaveFile))
         {
            return;
         }

         if (mergeAutoSave)
         {
            AutoSave.MergeChange();
         }
         else
         {
            AutoSave.Clear();
         }
      }

      public void Save()
      {
         if (User == null) throw new NullReferenceException(nameof(User));
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Passkeys = new[] { User.Username.GetHash() }.Union(User.Passkeys).ToArray();
         DatabaseFileLocker.WriteAllText(User.Serialize(), Passkeys);

         AutoSave.Clear();
      }

      public bool Login(string passkey)
      {
         Passkeys = Passkeys.Union(new[] { passkey }).ToArray();

         try
         {
            User = DatabaseFileLocker?.ReadAllText(Passkeys).Deserialize<User>();
         }
         catch { }

         if (User == null) return false;

         User.Database = this;

         if (File.Exists(AutoSaveFile))
         {
            AutoSaveFileLocker = new(AutoSaveFile, FileMode.Open);

            AutoSave = AutoSaveFileLocker.ReadAllText(Passkeys).Deserialize<AutoSave>();

            AutoSave.Database = this;
         }

         return true;
      }

      public void Close()
      {
         Dispose();
      }

      #endregion

      internal User? User;
      internal AutoSave AutoSave;

      internal string[] Passkeys { get; private set; }

      internal FileLocker? DatabaseFileLocker;
      internal FileLocker? AutoSaveFileLocker;

      private Database(string databaseFile, string autoSaveFile, string logFile, string username)
      {
         DatabaseFile = databaseFile;
         AutoSaveFile = autoSaveFile;
         LogFile = logFile;

         Passkeys = new[] { username.GetHash() };

         AutoSave = new()
         {
            Database = this,
         };
      }

      public static IDatabase Create(string databaseFile, string autoSaveFile, string logFile, string username, string[] passkeys)
      {
         if (File.Exists(databaseFile))
         {
            throw new IOException($"'{databaseFile}' database file already exists");
         }

         string databaseFileDirectory = Path.GetDirectoryName(databaseFile) ?? string.Empty;

         if (!Directory.Exists(databaseFileDirectory))
         {
            Directory.CreateDirectory(databaseFileDirectory);
         }

         Database database = new(databaseFile, autoSaveFile, logFile, username)
         {
            DatabaseFileLocker = new(databaseFile, FileMode.Create),
            Passkeys = new[] { username.GetHash() }.Union(passkeys).ToArray(),
         };

         database.User = new()
         {
            Database = database,
            ItemId = username.GetHash(),
            Username = username,
            Passkeys = passkeys,
         };

         database.Save();

         return database;
      }

      public static IDatabase Open(string databaseFile, string autoSaveFile, string logFile, string username)
      {
         Database database = new(databaseFile, autoSaveFile, logFile, username)
         {
            DatabaseFileLocker = new(databaseFile, FileMode.Open),
         };

         return database;
      }
   }
}