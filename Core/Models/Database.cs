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
      IWarning[]? IDatabase.Warnings => User != null ? Warnings : null;

      public void Delete()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         DatabaseFileLocker?.Delete();
         LogFileLocker?.Delete();
         AutoSaveFileLocker?.Delete();

         _close(logCloseEvent: false);
      }

      public void Dispose() => _close(logCloseEvent: true);

      public void Save() => _save(logSaveEvent: true);

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
               Logs.AddLog($"User {Username} login failed at level {(passwordException.PasswordLevel)}", true);
            }
         }

         if (User != null)
         {
            User.Database = this;

            Logs.AddLog($"User {Username} logged in", false);

            if (File.Exists(AutoSaveFile))
            {
               AutoSaveFileLocker = new(CryptographicCenter, SerializationCenter, AutoSaveFile, FileMode.Open);

               AutoSave = AutoSaveFileLocker.Open<AutoSave>(Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               _onAutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeBehavior);
            }

            Warning[] logWarnings = _lookAtLogWarnings();
            Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            Warning[] passwordLeakedWarnings = _lookAtPasswordLeakedWarnings();
            Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();

            Warnings = [..logWarnings,
               ..passwordUpdateReminderWarnings,
               ..passwordLeakedWarnings,
               ..duplicatedPasswordsWarnings];

            _onWarningDetected?.Invoke(this, new WarningDetectedEventArgs(
               [..User.WarningsToNotify.ContainsFlag(WarningType.LogReviewWarning) ? logWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordUpdateReminderWarning) ? passwordUpdateReminderWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordLeakedWarning) ? passwordLeakedWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.DuplicatedPasswordsWarning) ? duplicatedPasswordsWarnings : []]));
         }

         return User;
      }

      public void Close() => Dispose();

      #endregion

      internal User? User;
      internal AutoSave AutoSave;
      internal LogCenter Logs;
      internal Warning[]? Warnings;

      internal string Username { get; private set; }
      internal string[] Passkeys { get; private set; }

      internal FileLocker? DatabaseFileLocker;
      internal FileLocker? AutoSaveFileLocker;
      internal FileLocker? LogFileLocker;
      internal readonly ICryptographyCenter CryptographicCenter;
      internal readonly ISerializationCenter SerializationCenter;

      private readonly EventHandler<WarningDetectedEventArgs>? _onWarningDetected = null;
      private readonly EventHandler<AutoSaveDetectedEventArgs>? _onAutoSaveDetected = null;

      private Database(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         FileMode fileMode,
         EventHandler<WarningDetectedEventArgs>? warningDetectedHandler,
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

         Username = username;
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
         _onWarningDetected = warningDetectedHandler;
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
            warningDetectedHandler: null,
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

         database.Logs.AddLog($"User {username}'s database created", false);

         database._save(logSaveEvent: false);

         database._close(logCloseEvent: false);

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
         EventHandler<WarningDetectedEventArgs>? warningDetectedHandler = null,
         EventHandler<AutoSaveDetectedEventArgs>? autoSaveHandler = null)
      {
         Database database = new(cryptographicCenter,
            serializationCenter,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Open,
            warningDetectedHandler,
            autoSaveHandler,
            username);

         database.Logs.AddLog($"User {username}'s database opened", false);

         return database;
      }

      private void _save(bool logSaveEvent)
      {
         if (User == null) throw new NullReferenceException(nameof(User));
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Username = User.Username;
         Passkeys = [CryptographicCenter.GetHash(User.Username), .. User.Passkeys.Select(x => CryptographicCenter.GetSlowHash(x))];
         DatabaseFileLocker.Save(User, Passkeys);

         Logs.Username = Username;
         LogFileLocker?.Save(Logs, [CryptographicCenter.GetHash(User.Username)]);

         if (logSaveEvent)
         {
            Logs.AddLog($"User {Username}'s database saved", false);
         }

         AutoSave.Clear();
      }

      private void _close(bool logCloseEvent)
      {
         if (logCloseEvent)
         {
            if (User != null)
            {
               string logoutLog = $"User {Username} logged out";
               bool needsReview = AutoSave.Changes.Count != 0;

               if (needsReview)
               {
                  logoutLog += " without saving";
               }

               Logs.AddLog(logoutLog, needsReview);
            }

            Logs.AddLog($"User {Username}'s database closed", false);
         }

         User = null;
         AutoSave.Changes.Clear();
         Username = string.Empty;
         Passkeys = [];
         Warnings = null;

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
               Logs.AddLog($"User {Username}'s autosave merged", true);
               _save(logSaveEvent: false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear();
               Logs.AddLog($"User {Username}'s autosave not merged and removed", true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               Logs.AddLog($"User {Username}'s autosave not merged and keeped.", true);
               break;
         }
      }

      private Warning[] _lookAtLogWarnings()
      {
         if (User == null) throw new NullReferenceException(nameof(User));
         if (Logs.Logs == null) throw new NullReferenceException(nameof(Logs.Logs));

         List<Log> logs = Logs.Logs.Cast<Log>().ToList();

         for (int i = 0; i < logs.Count && logs[i].Message != $"User {Username} logged in"; i++)
         {
            if (!logs[i].NeedsReview
               || !logs[i].Message.StartsWith($"User {Username}'s autosave "))
            {
               logs.RemoveAt(i);
               i--;
            }
         }

         return [new Warning([.. logs.Where(x => x.NeedsReview)])];
      }

      private Warning[] _lookAtPasswordUpdateReminderWarnings()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         Account[] accounts = User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)
            .ToArray();

         if (accounts.Length != 0)
         {
            return [new Warning(WarningType.PasswordUpdateReminderWarning, accounts)];
         }
         else
         {
            return [];
         }
      }

      private Warning[] _lookAtPasswordLeakedWarnings()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         Account[] accounts = User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordLeaked)
            .ToArray();

         if (accounts.Length != 0)
         {
            return [new Warning(WarningType.PasswordLeakedWarning, accounts)];
         }
         else
         {
            return [];
         }
      }

      private Warning[] _lookAtDuplicatedPasswordsWarnings()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         IGrouping<string, Account>[] duplicatedPasswords = User.Services
            .SelectMany(x => x.Accounts)
            .GroupBy(x => x.Password)
            .Where(x => x.Count() > 1)
            .ToArray();

         List<Warning> warnings = [];

         foreach (IGrouping<string, Account> accounts in duplicatedPasswords)
         {
            warnings.Add(new(WarningType.DuplicatedPasswordsWarning, [.. accounts.Cast<Account>()]));
         }

         return [.. warnings];
      }
   }
}