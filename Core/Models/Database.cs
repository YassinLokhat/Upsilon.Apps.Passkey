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

      IUser? IDatabase.User => User;
      ILog[]? IDatabase.Logs => Logs.Logs;

      public void Delete()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         DatabaseFileLocker?.Delete();
         LogFileLocker?.Delete();
         AutoSaveFileLocker?.Delete();

         Dispose();
      }

      public void Dispose()
      {
         if (User != null)
         {
            Logs.AddLog(User.ItemId, $"logged out", false);
         }

         User = null;
         AutoSave.Changes.Clear();
         Passkeys = [];

         DatabaseFileLocker?.Dispose();
         DatabaseFileLocker = null;

         LogFileLocker?.Dispose();
         LogFileLocker = null;

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

         Logs.Username = User.Username;
         LogFileLocker?.Save(Logs, [CryptographicCenter.GetHash(User.Username)]);
         Logs.AddLog(User.ItemId, $"database saved", false);

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
         catch (Exception ex)
         {
            if (ex is WrongPasswordException passwordException)
            {
               Logs.AddLog(string.Empty, $"login failed at level {(passwordException.PasswordLevel)}", true);
            }
         }

         if (User != null)
         {
            User.Database = this;

            Logs.AddLog(User.ItemId, $"logged in", false);

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
      internal LogCenter Logs;

      internal string[] Passkeys { get; private set; }

      internal FileLocker? DatabaseFileLocker;
      internal FileLocker? AutoSaveFileLocker;
      internal FileLocker? LogFileLocker;
      internal readonly ICryptographyCenter CryptographicCenter;
      internal readonly ISerializationCenter SerializationCenter;

      private readonly EventHandler<AutoSaveDetectedEventArgs>? _onAutoSaveDetected = null;

      private Database(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         FileMode fileMode,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler,
         string username,
         string publicKey = "",
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

         LogFileLocker = new(cryptographicCenter, serializationCenter, logFile, fileMode);

         Logs = fileMode == FileMode.Create
            ? new()
            {
               Username = username,
               PublicKey = publicKey,
            }
            : LogFileLocker.Open<LogCenter>([cryptographicCenter.GetHash(username)]);

         Logs.Database = this;

         _onAutoSaveDetected = autoSaveHandler;
      }

      internal static IDatabase Create(ICryptographyCenter cryptographicCenter,
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

         cryptographicCenter.GenerateRandomKeys(out string publicKey, out string privateKey);

         Database database = new(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Create,
            autoSaveHandler: null,
            username,
            publicKey,
            passkeys);

         database.User = new()
         {
            Database = database,
            PrivateKey = privateKey,
            ItemId = cryptographicCenter.GetHash(username),
            Username = username,
            Passkeys = [.. passkeys],
         };

         database.Logs.AddLog(database.User.ItemId, $"database created", false);

         database.Save();

         database.Dispose();

         return Open(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            username);
      }

      internal static IDatabase Open(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler = null)
      {
         Database database = new(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Open,
            autoSaveHandler,
            username);

         database.Logs.AddLog(username, $"database opened", false);

         return database;
      }

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
               Logs.AddLog(User.ItemId, $"autosave merged and removed", false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear();
               Logs.AddLog(User.ItemId, $"autosave not merged and removed", false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               Logs.AddLog(User.ItemId, $"autosave not merged and keeped.", false);
               break;
         }
      }
   }
}