using System;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   public sealed class Database : IDatabase
   {
      #region IUser interface explicit Internal

      public string DatabaseFile { get; set; }

      IUser? IDatabase.User => Get(User);
      int? IDatabase.SessionLeftTime => User?.SessionLeftTime;

      ILog[]? IDatabase.Logs => Get(Logs.Logs.OrderByDescending(x => x.DateTime).ToArray());

      IWarning[]? IDatabase.Warnings => Get(User is not null ? Warnings : null);

      public ICryptographyCenter CryptographyCenter { get; private set; }
      public ISerializationCenter SerializationCenter { get; private set; }
      public IPasswordFactory PasswordFactory { get; private set; }
      public IClipboardManager ClipboardManager { get; private set; }

      public event EventHandler<WarningDetectedEventArgs>? WarningDetected;
      public event EventHandler<AutoSaveDetectedEventArgs>? AutoSaveDetected;
      public event EventHandler? DatabaseSaved;
      public event EventHandler<LogoutEventArgs>? DatabaseClosed;

      public void Delete()
      {
         if (User is null) throw new NullReferenceException(nameof(User));

         FileLocker.Delete();

         Close(logCloseEvent: false, loginTimeoutReached: false);
      }

      public void Dispose() => Close(logCloseEvent: true, loginTimeoutReached: false);

      public void Save() => _save(logSaveEvent: true);

      public IUser? Login(string passkey)
      {
         Passkeys = [.. Passkeys, CryptographyCenter.GetSlowHash(passkey)];

         try
         {
            User = FileLocker.Open<User>(DatabaseFileEntry, Passkeys);
         }
         catch (Exception ex)
         {
            if (ex is WrongPasswordException passwordException)
            {
               Logs.AddLog(data: [Username, passwordException.PasswordLevel.ToString()],
                  eventType: LogEventType.LoginFailed,
                  needsReview: true);
            }
         }

         if (User is not null)
         {
            User.Database = this;

            Logs.LoadStringLogs();
            Logs.AddLog(data: [Username],
               eventType: LogEventType.UserLoggedIn,
               needsReview: false);

            if (FileLocker.Exists(AutoSaveFileEntry))
            {
               AutoSave = FileLocker.Open<AutoSave>(AutoSaveFileEntry, Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               AutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeBehavior);
            }

            _ = Task.Run(_lookAtWarnings);

            User.ResetTimer();
         }

         return User;
      }

      public void Close() => Dispose();

      public bool HasChanged() => User is not null && HasChanged(User.ItemId);

      public bool HasChanged(string itemId) => AutoSave.Any(itemId);

      public bool HasChanged(string itemId, string fieldName) => AutoSave.Any(itemId, fieldName);

      public bool ImportFromFile(string filePath)
      {
         _save(logSaveEvent: true);
         Logs.AddLog(data: [filePath],
            eventType: LogEventType.ImportingDataStarted,
            needsReview: true);

         string importContent = string.Empty;
         string errorLog = string.Empty;

         try
         {
            importContent = File.ReadAllText(filePath);
         }
         catch
         {
            errorLog = $"import file is not accessible";
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            string extention = Path.GetExtension(filePath);

            errorLog = extention switch
            {
               ".json" => this.ImportJson(importContent),
               ".csv" => this.ImportCSV(importContent),
               _ => $"'{extention}' extention type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            Logs.AddLog(data: [],
               eventType: LogEventType.ImportingDataSucceded,
               needsReview: true);
            _save(logSaveEvent: true);
         }
         else
         {
            Logs.AddLog(data: [errorLog],
               eventType: LogEventType.ImportingDataFailed,
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      public bool ExportToFile(string filePath)
      {
         _save(logSaveEvent: true);
         Logs.AddLog(data: [filePath],
            eventType: LogEventType.ExportingDataStarted,
            needsReview: true);

         string errorLog = string.Empty;

         if (File.Exists(filePath))
         {
            errorLog = $"export file already exists";
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            string extention = Path.GetExtension(filePath);

            errorLog = extention switch
            {
               ".json" => this.ExportJson(filePath),
               ".csv" => this.ExportCSV(filePath),
               _ => $"'{extention}' extention type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            Logs.AddLog(data: [],
               eventType: LogEventType.ExportingDataSucceded,
               needsReview: true);
         }
         else
         {
            Logs.AddLog(data: [errorLog],
               eventType: LogEventType.ExportingDataFailed,
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      #endregion

      internal User? User;
      internal AutoSave AutoSave;
      internal LogCenter Logs;
      internal Warning[]? Warnings;

      internal string Username { get; private set; }
      internal string[] Passkeys { get; private set; }

      internal readonly string DatabaseFileEntry = "database";
      internal readonly string AutoSaveFileEntry = "autosave";
      internal readonly string LogFileEntry = "log";
      internal FileLocker FileLocker;

      private Database(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         FileMode fileMode,
         string username,
         string publicKey = "",
         string[]? passkeys = null)
      {
         DatabaseFile = databaseFile;

         CryptographyCenter = cryptographicCenter;
         SerializationCenter = serializationCenter;
         PasswordFactory = passwordFactory;
         ClipboardManager = clipboardManager;

         Username = username;
         Passkeys = [CryptographyCenter.GetHash(username)];

         if (passkeys is not null)
         {
            Passkeys = [.. Passkeys, .. passkeys.Select(x => CryptographyCenter.GetSlowHash(x))];
         }

         AutoSave = new()
         {
            Database = this,
         };

         FileLocker = new(cryptographicCenter, serializationCenter, databaseFile, fileMode);

         Logs = fileMode == FileMode.Create
            ? new()
            {
               Username = username,
               PublicKey = publicKey,
            }
            : FileLocker.Open<LogCenter>(LogFileEntry);

         Logs.Database = this;
      }

      public static IDatabase Create(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
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
            passwordFactory,
            clipboardManager,
            databaseFile,
            FileMode.Create,
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

         database.Logs.AddLog(data: [username],
            eventType: LogEventType.DatabaseCreated,
            needsReview: false);

         database._save(logSaveEvent: false);

         return database;
      }

      public static IDatabase Open(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username)
      {
         Database database = new(cryptographicCenter,
            serializationCenter,
            passwordFactory,
            clipboardManager,
            databaseFile,
            FileMode.Open,
            username);

         database.Logs.AddLog(data: [username],
            eventType: LogEventType.DatabaseOpened,
            needsReview: false);

         return database;
      }

      internal T Get<T>(T value)
      {
         User?.ResetTimer();

         return value;
      }

      private void _save(bool logSaveEvent)
      {
         _saveLogs();
         _saveDatabase(logSaveEvent);
      }

      private void _saveDatabase(bool logSaveEvent)
      {
         if (User is null) throw new NullReferenceException(nameof(User));

         Username = User.Username;
         Passkeys = [CryptographyCenter.GetHash(User.Username), .. User.Passkeys.Select(CryptographyCenter.GetSlowHash)];
         FileLocker.Save(User, DatabaseFileEntry, Passkeys);

         if (logSaveEvent)
         {
            Logs.AddLog(data: [Username],
               eventType: LogEventType.DatabaseSaved,
               needsReview: false);
         }

         AutoSave.Clear(deleteFile: true);

         User.ResetTimer();

         DatabaseSaved?.Invoke(this, EventArgs.Empty);
      }

      private void _saveLogs()
      {
         if (User is null) throw new NullReferenceException(nameof(User));

         Logs.Username = User.Username;
         FileLocker.Save(Logs, LogFileEntry, [CryptographyCenter.GetHash(User.Username)]);
      }

      internal void Close(bool logCloseEvent, bool loginTimeoutReached)
      {
         if (logCloseEvent)
         {
            if (User is not null)
            {
               bool needsReview = AutoSave.Any();

               if (!needsReview)
               {
                  AutoSave.Clear(deleteFile: true);
               }

               Logs.AddLog(data: [Username, needsReview ? "1" : string.Empty],
                  eventType: LogEventType.UserLoggedOut,
                  needsReview);
            }

            Logs.AddLog(data: [Username],
               eventType: LogEventType.DatabaseClosed,
               needsReview: false);
         }

         User = null;
         Username = string.Empty;
         Passkeys = [];
         Warnings = null;

         FileLocker.Dispose();

         DatabaseClosed?.Invoke(this, new(loginTimeoutReached));
      }

      private void _handleAutoSave(AutoSaveMergeBehavior mergeAutoSave)
      {
         if (User is null) throw new NullReferenceException(nameof(User));

         if (!FileLocker.Exists(AutoSaveFileEntry))
         {
            return;
         }

         switch (mergeAutoSave)
         {
            case AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: true);
               _save(logSaveEvent: false);
               break;
            case AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile:
               AutoSave.ApplyChanges(deleteFile: false);
               _saveLogs();
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear(deleteFile: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               break;
         }

         Logs.AddLog(data: [Username],
            eventType: (LogEventType)mergeAutoSave,
            needsReview: true);
      }

      private void _lookAtWarnings()
      {
         if (User is null) return;

         try
         {
            Warning[] logWarnings = _lookAtLogWarnings();
            Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            Warning[] passwordLeakedWarnings = _lookAtPasswordLeakedWarnings();
            Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();

            Warnings = [..logWarnings,
               ..passwordUpdateReminderWarnings,
               ..passwordLeakedWarnings,
               ..duplicatedPasswordsWarnings];

            WarningDetected?.Invoke(this, new WarningDetectedEventArgs(
               [..User.WarningsToNotify.ContainsFlag(WarningType.LogReviewWarning) ? logWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordUpdateReminderWarning) ? passwordUpdateReminderWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordLeakedWarning) ? passwordLeakedWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.DuplicatedPasswordsWarning) ? duplicatedPasswordsWarnings : []]));
         }
         catch { }
      }

      private Warning[] _lookAtLogWarnings()
      {
         if (User is null) throw new NullReferenceException(nameof(User));
         if (Logs.Logs is null) throw new NullReferenceException(nameof(Logs.Logs));

         List<Log> logs = [.. Logs.Logs.Cast<Log>()];

         //for (int i = 0; i < logs.Count && logs[i].EventType != LogEventType.UserLoggedIn; i++)
         //{
         //   if (!logs[i].NeedsReview
         //      || (logs[i].EventType != LogEventType.MergeAndSaveThenRemoveAutoSaveFile
         //         && logs[i].EventType != LogEventType.MergeWithoutSavingAndKeepAutoSaveFile
         //         && logs[i].EventType != LogEventType.DontMergeAndRemoveAutoSaveFile
         //         && logs[i].EventType != LogEventType.DontMergeAndKeepAutoSaveFile))
         //   {
         //      logs.RemoveAt(i);
         //      i--;
         //   }
         //}

         return [new Warning([.. logs.Where(x => x.NeedsReview)])];
      }

      private Warning[] _lookAtPasswordUpdateReminderWarnings()
      {
         if (User is null) return [];

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)];

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordUpdateReminderWarning, accounts)] : [];
      }

      private Warning[] _lookAtPasswordLeakedWarnings()
      {
         if (User is null) return [];

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordLeaked)];

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordLeakedWarning, accounts)] : [];
      }

      private Warning[] _lookAtDuplicatedPasswordsWarnings()
      {
         if (User is null) return [];

         IGrouping<string, Account>[] duplicatedPasswords = [.. User.Services
            .SelectMany(x => x.Accounts)
            .GroupBy(x => x.Password)
            .Where(x => x.Count() > 1)];

         List<Warning> warnings = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            warnings.Add(new(WarningType.DuplicatedPasswordsWarning, [.. accounts.Cast<Account>()]));
         }

         return [.. warnings];
      }
   }
}