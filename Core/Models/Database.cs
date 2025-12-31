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

      IUser? IDatabase.User => User;
      int? IDatabase.SessionLeftTime => User?.SessionLeftTime;

      IActivity[]? IDatabase.Activities => Get(ActivityCenter.Activities.OrderByDescending(x => x.DateTime).ToArray());

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
               ActivityCenter.AddActiivity(itemId: string.Empty,
                  eventType: ActivityEventType.LoginFailed,
                  data: [Username, passwordException.PasswordLevel.ToString()],
                  needsReview: true);
            }
         }

         if (User is not null)
         {
            User.Database = this;

            ActivityCenter.LoadStringActivities();
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.UserLoggedIn,
               data: [Username],
               needsReview: false);

            if (FileLocker.Exists(AutoSaveFileEntry))
            {
               AutoSave = FileLocker.Open<AutoSave>(AutoSaveFileEntry, Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               AutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeBehavior);
            }

            _lookAtWarnings();

            User.ResetTimer();
         }

         return User;
      }

      public void Close() => Dispose();

      public bool HasChanged(string itemId) => AutoSave.Any(itemId);

      public bool HasChanged(string itemId, string fieldName) => AutoSave.Any(itemId, fieldName);

      public bool ImportFromFile(string filePath)
      {
         _save(logSaveEvent: true);
         ActivityCenter.AddActiivity(itemId: string.Empty,
            eventType: ActivityEventType.ImportingDataStarted,
            data: [filePath],
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
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataSucceded,
               data: [],
               needsReview: true);
            _save(logSaveEvent: true);
         }
         else
         {
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      public bool ExportToFile(string filePath)
      {
         _save(logSaveEvent: true);
         ActivityCenter.AddActiivity(itemId: string.Empty,
            eventType: ActivityEventType.ExportingDataStarted,
            data: [filePath],
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
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataSucceded,
               data: [],
               needsReview: true);
         }
         else
         {
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      #endregion

      internal User? User;
      internal AutoSave AutoSave;
      internal ActivityCenter ActivityCenter;
      internal Warning[]? Warnings;

      internal string Username { get; private set; }
      internal string[] Passkeys { get; private set; }

      internal readonly string DatabaseFileEntry = "database";
      internal readonly string AutoSaveFileEntry = "autosave";
      internal readonly string ActivityFileEntry = "activity";
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

         ActivityCenter = fileMode == FileMode.Create
            ? new()
            {
               Username = username,
               PublicKey = publicKey,
            }
            : FileLocker.Open<ActivityCenter>(ActivityFileEntry);

         ActivityCenter.Database = this;
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
            ItemId = "U" + cryptographicCenter.GetHash(username),
            Username = username,
            Passkeys = [.. passkeys],
         };

         database.ActivityCenter.AddActiivity(itemId: string.Empty,
            eventType: ActivityEventType.DatabaseCreated,
            data: [username],
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

         database.ActivityCenter.AddActiivity(itemId: string.Empty,
            eventType: ActivityEventType.DatabaseOpened,
            data: [username],
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
         _saveActivities();
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
            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseSaved,
               data: [Username],
               needsReview: false);
         }

         AutoSave.Clear(deleteFile: true);

         _lookAtWarnings();

         User.ResetTimer();

         DatabaseSaved?.Invoke(this, EventArgs.Empty);
      }

      private void _saveActivities()
      {
         if (User is null) throw new NullReferenceException(nameof(User));

         ActivityCenter.Username = User.Username;
         ActivityCenter.Save(rebuildStringActivities: true);
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

               ActivityCenter.AddActiivity(itemId: string.Empty,
                  eventType: ActivityEventType.UserLoggedOut,
                  data: [Username, needsReview ? "1" : string.Empty],
                  needsReview);
            }

            ActivityCenter.AddActiivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseClosed,
               data: [Username],
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
               _saveActivities();
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear(deleteFile: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               break;
         }

         ActivityCenter.AddActiivity(itemId: string.Empty,
            eventType: (ActivityEventType)mergeAutoSave,
            data: [Username],
            needsReview: true);
      }

      private void _lookAtWarnings()
      {
         if (User is null) return;

         Warning[] activityWarnings = _lookAtActivityWarnings();
         Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
         Warning[] passwordLeakedWarnings = _lookAtPasswordLeakedWarnings();
         Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();

         Warnings = [..activityWarnings,
               ..passwordUpdateReminderWarnings,
               ..passwordLeakedWarnings,
               ..duplicatedPasswordsWarnings];

         WarningDetected?.Invoke(this, new WarningDetectedEventArgs(
            [..User.WarningsToNotify.ContainsFlag(WarningType.ActivityReviewWarning) ? activityWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordUpdateReminderWarning) ? passwordUpdateReminderWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.PasswordLeakedWarning) ? passwordLeakedWarnings : [],
               ..User.WarningsToNotify.ContainsFlag(WarningType.DuplicatedPasswordsWarning) ? duplicatedPasswordsWarnings : []]));
      }

      private Warning[] _lookAtActivityWarnings()
      {
         return User is null
            ? throw new NullReferenceException(nameof(User))
            : ActivityCenter.Activities is null
            ? throw new NullReferenceException(nameof(ActivityCenter.Activities))
            : [new Warning([.. ActivityCenter.Activities.Where(x => x.NeedsReview).Cast<Activity>()])];
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
            .AsParallel()
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