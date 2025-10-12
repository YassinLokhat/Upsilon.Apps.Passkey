using Upsilon.Apps.PassKey.Core.Internal.Utils;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Events;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;
using Upsilon.Apps.PassKey.Core.Public.Utils;

namespace Upsilon.Apps.PassKey.Core.Internal.Models
{
   internal sealed class Database : IDatabase
   {
      #region IUser interface explicit Internal

      public string DatabaseFile { get; set; }
      public string AutoSaveFile { get; set; }
      public string LogFile { get; set; }

      IUser? IDatabase.User => Get(User);

      ILog[]? IDatabase.Logs => Get(Logs.Logs);

      IWarning[]? IDatabase.Warnings => Get(User != null ? Warnings : null);

      public ICryptographyCenter CryptographyCenter { get; private set; }
      public ISerializationCenter SerializationCenter { get; private set; }
      public IPasswordFactory PasswordFactory { get; private set; }

      public event EventHandler<WarningDetectedEventArgs>? WarningDetected;
      public event EventHandler<AutoSaveDetectedEventArgs>? AutoSaveDetected;
      public event EventHandler? DatabaseSaved;
      public event EventHandler<LogoutEventArgs>? DatabaseClosed;

      public void Delete()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         DatabaseFileLocker?.Delete();
         LogFileLocker?.Delete();
         AutoSaveFileLocker?.Delete();

         Close(logCloseEvent: false, loginTimeoutReached: false);
      }

      public void Dispose() => Close(logCloseEvent: true, loginTimeoutReached: false);

      public void Save() => _save(logSaveEvent: true);

      public IUser? Login(string passkey)
      {
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Passkeys = [.. Passkeys, CryptographyCenter.GetSlowHash(passkey)];

         try
         {
            User = DatabaseFileLocker.Open<User>(Passkeys);
         }
         catch (Exception ex)
         {
            if (ex is WrongPasswordException passwordException)
            {
               Logs.AddLog($"User {Username} login failed at level {passwordException.PasswordLevel}", needsReview: true);
            }
         }

         if (User != null)
         {
            User.Database = this;

            Logs.AddLog($"User {Username} logged in", needsReview: false);

            if (File.Exists(AutoSaveFile))
            {
               AutoSaveFileLocker = new(CryptographyCenter, SerializationCenter, AutoSaveFile, FileMode.Open);

               AutoSave = AutoSaveFileLocker.Open<AutoSave>(Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               AutoSaveDetected?.Invoke(this, eventArg);
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

            WarningDetected?.Invoke(this, new WarningDetectedEventArgs(
               [..User.WarningsToNotify.ContainsFlag(WarningType.LogReviewWarning) ? logWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordUpdateReminderWarning) ? passwordUpdateReminderWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordLeakedWarning) ? passwordLeakedWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.DuplicatedPasswordsWarning) ? duplicatedPasswordsWarnings : []]));

            User.ResetTimer();
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

      private Database(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         FileMode fileMode,
         string username,
         string publicKey = "",
         string[]? passkeys = null)
      {
         DatabaseFile = databaseFile;
         AutoSaveFile = autoSaveFile;
         LogFile = logFile;

         CryptographyCenter = cryptographicCenter;
         SerializationCenter = serializationCenter;
         PasswordFactory = passwordFactory;

         Username = username;
         Passkeys = [CryptographyCenter.GetHash(username)];

         if (passkeys != null)
         {
            Passkeys = [.. Passkeys, .. passkeys.Select(x => CryptographyCenter.GetSlowHash(x))];
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
      }

      internal static IDatabase Create(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
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
            passwordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
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

         database.Logs.AddLog($"User {username}'s database created", needsReview: false);

         database._save(logSaveEvent: false);

         database.Close(logCloseEvent: false, loginTimeoutReached: false);

         return Open(cryptographicCenter,
            serializationCenter,
            passwordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            username);
      }

      internal static IDatabase Open(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         string databaseFile,
         string autoSaveFile,
         string logFile,
         string username)
      {
         Database database = new(cryptographicCenter,
            serializationCenter,
            passwordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            FileMode.Open,
            username);

         database.Logs.AddLog($"User {username}'s database opened", needsReview: false);

         return database;
      }

      internal T Get<T>(T value)
      {
         User?.ResetTimer();

         return value;
      }

      private void _save(bool logSaveEvent)
      {
         if (User == null) throw new NullReferenceException(nameof(User));
         if (DatabaseFileLocker == null) throw new NullReferenceException(nameof(DatabaseFileLocker));

         Username = User.Username;
         Passkeys = [CryptographyCenter.GetHash(User.Username), .. User.Passkeys.Select(x => CryptographyCenter.GetSlowHash(x))];
         DatabaseFileLocker.Save(User, Passkeys);

         Logs.Username = Username;
         LogFileLocker?.Save(Logs, [CryptographyCenter.GetHash(User.Username)]);

         if (logSaveEvent)
         {
            Logs.AddLog($"User {Username}'s database saved", needsReview: false);
         }

         AutoSave.Clear();

         User.ResetTimer();

         DatabaseSaved?.Invoke(this, EventArgs.Empty);
      }

      internal void Close(bool logCloseEvent, bool loginTimeoutReached)
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

            Logs.AddLog($"User {Username}'s database closed", needsReview: false);
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

         DatabaseClosed?.Invoke(this, new(loginTimeoutReached));
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
               Logs.AddLog($"User {Username}'s autosave merged", needsReview: true);
               _save(logSaveEvent: false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear();
               Logs.AddLog($"User {Username}'s autosave not merged and removed", needsReview: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               Logs.AddLog($"User {Username}'s autosave not merged and keeped.", needsReview: true);
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

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordUpdateReminderWarning, accounts)] : [];
      }

      private Warning[] _lookAtPasswordLeakedWarnings()
      {
         if (User == null) throw new NullReferenceException(nameof(User));

         Account[] accounts = User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordLeaked)
            .ToArray();

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordLeakedWarning, accounts)] : [];
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