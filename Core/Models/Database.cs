using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.PassKey.Core.Events;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal sealed class Database : IDatabase
   {
      #region IUser interface explicit implementation

      public string DatabaseFile { get; set; }
      public string AutoSaveFile { get; set; }
      public string LogFile { get; set; }

      IUser? IDatabase.User { get => User; }

      public void Delete()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

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
         Passkeys = [];

         DatabaseFileLocker?.Dispose();
         DatabaseFileLocker = null;

         AutoSaveFileLocker?.Dispose();
         AutoSaveFileLocker = null;

         DatabaseFile = string.Empty;
         AutoSaveFile = string.Empty;
         LogFile = string.Empty;
      }

      public void Save()
      {
         if (User == null) throw new NullReferenceException(nameof(User));
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Passkeys = [User.Username.GetHash(), .. User.Passkeys];
         DatabaseFileLocker.WriteAllText(User.Serialize(), Passkeys);

         AutoSave.Clear();
      }

      public IUser? Login(string passkey)
      {
         Passkeys = [.. Passkeys, passkey];

         try
         {
            User = DatabaseFileLocker?.ReadAllText(Passkeys).Deserialize<User>();
         }
         catch
         {
            // TODO : Log the error
         }

         if (User != null)
         {
            User.Database = this;

            if (File.Exists(AutoSaveFile))
            {
               AutoSaveFileLocker = new(AutoSaveFile, FileMode.Open);

               AutoSave = AutoSaveFileLocker.ReadAllText(Passkeys).Deserialize<AutoSave>();
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               _onAutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeAutoSave);
            }
         }

         return User;
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

      private EventHandler<AutoSaveDetectedEventArgs>? _onAutoSaveDetected = null;

      private Database(string databaseFile, string autoSaveFile, string logFile, FileMode fileMode, EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler, string username, string[]? passkeys = null)
      {
         DatabaseFile = databaseFile;
         AutoSaveFile = autoSaveFile;
         LogFile = logFile;

         Passkeys = [username.GetHash()];

         if (passkeys != null)
         {
            Passkeys = [.. Passkeys, .. passkeys];
         }

         AutoSave = new()
         {
            Database = this,
         };

         DatabaseFileLocker = new(databaseFile, fileMode);

         _onAutoSaveDetected = autoSaveHandler;
      }

      internal static IDatabase Create(string databaseFile, string autoSaveFile, string logFile, string username, string[] passkeys)
      {
         if (File.Exists(databaseFile))
         {
            throw new IOException($"'{databaseFile}' database file already exists");
         }

         string databaseFileDirectory = Path.GetDirectoryName(databaseFile) ?? string.Empty;

         if (!Directory.Exists(databaseFileDirectory))
         {
            _ = Directory.CreateDirectory(databaseFileDirectory);
         }

         Database database = new(databaseFile, autoSaveFile, logFile, FileMode.Create, autoSaveHandler: null, username, passkeys);

         database.User = new()
         {
            Database = database,
            ItemId = username.GetHash(),
            Username = username,
            Passkeys = passkeys,
         };

         database.Save();

         database.Dispose();

         return Open(databaseFile, autoSaveFile, logFile, username);
      }

      internal static IDatabase Open(string databaseFile, string autoSaveFile, string logFile, string username, EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler = null)
         => new Database(databaseFile, autoSaveFile, logFile, FileMode.Open, autoSaveHandler, username);

      private void _handleAutoSave(bool mergeAutoSave)
      {
         if (User == null) throw new NullReferenceException(nameof(User));

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
   }
}