using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Events;
using Upsilon.Apps.PassKey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.PassKey.Core.Models
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

         Passkeys = [CryptographicCenter.GetHash(User.Username), .. User.Passkeys.Select(x => CryptographicCenter.GetSlowHash(x))];
         DatabaseFileLocker.Save(User, Passkeys);

         AutoSave.Clear();
      }

      public IUser? Login(string passkey)
      {
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Passkeys = [.. Passkeys, CryptographicCenter.GetSlowHash(passkey)];

         try
         {
            User = DatabaseFileLocker.Open<User>(Passkeys);
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
               AutoSaveFileLocker = new(CryptographicCenter, SerializationCenter, AutoSaveFile, FileMode.Open);

               AutoSave = AutoSaveFileLocker.Open<AutoSave>(Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               _onAutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeBehavior);
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
      internal readonly ICryptographicCenter CryptographicCenter;
      internal readonly ISerializationCenter SerializationCenter;

      private readonly EventHandler<AutoSaveDetectedEventArgs>? _onAutoSaveDetected = null;

      private Database(ICryptographicCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         FileMode fileMode,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler,
         string username,
         string[]? passkeys = null)
      {
         DatabaseFile = databaseFile;
         AutoSaveFile = autoSaveFile;
         LogFile = logFile;

         CryptographicCenter = cryptographicCenter;
         SerializationCenter = serializationCenter;

         Passkeys = [CryptographicCenter.GetHash(username)];

         if (passkeys != null)
         {
            Passkeys = [.. Passkeys, .. passkeys.Select(x => CryptographicCenter.GetSlowHash(x))];
         }

         AutoSave = new()
         {
            Database = this,
         };

         DatabaseFileLocker = new(cryptographicCenter, serializationCenter, databaseFile, fileMode);

         _onAutoSaveDetected = autoSaveHandler;
      }

      internal static IDatabase Create(ICryptographicCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         string[] passkeys)
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

         Database database = new(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Create,
            autoSaveHandler: null,
            username,
            passkeys);

         database.User = new()
         {
            Database = database,
            ItemId = cryptographicCenter.GetHash(username),
            Username = username,
            Passkeys = [.. passkeys],
         };

         database.Save();

         database.Dispose();

         return Open(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            username);
      }

      internal static IDatabase Open(ICryptographicCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler = null)
         => new Database(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Open,
            autoSaveHandler,
            username);

      private void _handleAutoSave(AutoSaveMergeBehavior mergeAutoSave)
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         if (!File.Exists(AutoSaveFile))
         {
            return;
         }

         switch (mergeAutoSave)
         {
            case AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile:
               AutoSave.MergeChange();
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear();
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               break;
         }
      }
   }
}