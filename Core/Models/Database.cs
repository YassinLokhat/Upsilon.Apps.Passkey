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

      IEnumerable<IActivity>? IDatabase.Activities => Get(ActivityCenter.Activities.OrderByDescending(x => x.DateTime).ToArray());

      IEnumerable<IWarning>? IDatabase.Warnings => Get(User is not null ? Warnings : null);

      public ICryptographyCenter CryptographyCenter { get; private set; }
      public ISerializationCenter SerializationCenter { get; private set; }
      public IPasswordFactory PasswordFactory { get; private set; }
      public IClipboardManager ClipboardManager { get; private set; }

      public event EventHandler<WarningsUpdatedEventArgs>? WarningsUpdated;
      public event EventHandler<AutoSaveDetectedEventArgs>? AutoSaveDetected;
      public event EventHandler? DatabaseSaved;
      public event EventHandler<LogoutEventArgs>? DatabaseClosed;

      public void Delete()
      {
         if (User is null) throw new NullValueException(nameof(User));

         // Drop any debounced write before erasing the file so a late timer
         // cannot recreate entries on a path that no longer exists.
         AutoSave.Clear(deleteFile: false);
         ActivityCenter.CancelPending();

         FileLocker.Delete();

         Close(logCloseEvent: false, loginTimeoutReached: false);
      }

      public void Dispose() => Close(logCloseEvent: true, loginTimeoutReached: false);

      public void Save() => _save(logSaveEvent: true);

      // Progressive onion login: each call appends a stretched passkey and never
      // rolls back on failure. A wrong attempt poisons the stack until Close/Open,
      // which is deliberate online brute-force friction (see SECURITY.md).
      public IUser? Login(string passkey)
      {
         Passkeys = [.. Passkeys, CryptographyCenter.GetSlowHash(passkey, _slowHashParameters)];

         try
         {
            User = FileLocker.Open<User>(DatabaseFileEntry, Passkeys);
         }
         catch (WrongPasswordException passwordException)
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.LoginFailed,
               data: [Username, $"{passwordException.PasswordLevel}"],
               needsReview: true);
         }
#pragma warning disable CA1031 // Last-resort barrier: an unexpected login failure is traced, not propagated
         catch (Exception ex)
#pragma warning restore CA1031
         {
            System.Diagnostics.Trace.TraceWarning($"Unexpected error during login :\n{ex.Message}");
         }

         if (User is not null)
         {
            User.Database = this;

            ActivityCenter.LoadStringActivities();

            // Assert the log's sealed portion is intact now that the private key
            // (the verification anchor) is available. On failure we record a
            // reviewable activity rather than blocking access, so the user is
            // alerted while still being able to log in.
            if (!ActivityCenter.VerifyIntegrity())
            {
               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: ActivityEventType.ActivityLogTampered,
                  data: [Username],
                  needsReview: true);
            }

            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.UserLoggedIn,
               data: [Username],
               needsReview: false);

            if (FileLocker.Exists(AutoSaveFileEntry))
            {
               AutoSave.Dispose();
               AutoSave = FileLocker.Open<AutoSave>(AutoSaveFileEntry, Passkeys);
               AutoSave.Database = this;

               AutoSaveDetectedEventArgs eventArg = new();
               AutoSaveDetected?.Invoke(this, eventArg);
               _handleAutoSave(eventArg.MergeBehavior);
            }

            _ = Task.Run(_lookAtWarningsAsync);

            User.ResetTimer();
         }

         return User;
      }

      public void Close() => Dispose();

      public bool HasChanged(string itemId) => AutoSave.Any(itemId);

      public bool HasChanged(string itemId, string fieldName) => AutoSave.Any(itemId, fieldName);

      public bool ImportFromFile(string filePath)
      {
         if (User is null) throw new NullValueException(nameof(User));

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         ActivityCenter.AddActivity(itemId: string.Empty,
            eventType: ActivityEventType.ImportingDataStarted,
            data: [filePath],
            needsReview: true);

         string importContent = string.Empty;
         string errorLog = string.Empty;

         try
         {
            importContent = File.ReadAllText(filePath);
         }
#pragma warning disable CA1031 // Intentional: any file access failure is reported as a user-facing error message
         catch
#pragma warning restore CA1031
         {
            errorLog = $"import file is not accessible";
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ImportJson(importContent),
               ".csv" => this.ImportCSV(importContent),
               _ => $"'{extension}' extension type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataSucceded,
               data: [],
               needsReview: true);
            _save(logSaveEvent: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ImportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      public bool ExportToFile(string filePath)
      {
         if (User is null) throw new NullValueException(nameof(User));

         if (User.HasChanged())
         {
            _save(logSaveEvent: true);
         }

         ActivityCenter.AddActivity(itemId: string.Empty,
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
            string extension = Path.GetExtension(filePath);

            errorLog = extension switch
            {
               ".json" => this.ExportJson(filePath),
               ".csv" => this.ExportCSV(filePath),
               _ => $"'{extension}' extension type is not handled",
            };
         }

         if (string.IsNullOrWhiteSpace(errorLog))
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataSucceded,
               data: [],
               needsReview: true);
         }
         else
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.ExportingDataFailed,
               data: [errorLog],
               needsReview: true);
         }

         return string.IsNullOrWhiteSpace(errorLog);
      }

      #endregion

      #region Asynchronous entry points

      // Everything the database does is CPU- and disk-bound: stretching passkeys,
      // peeling AES layers, re-encrypting the activity log, rewriting the ZIP.
      // There is no naturally asynchronous work to await underneath, so these
      // entry points simply hand the synchronous operation to the thread pool and
      // give the caller something to await. The point is not throughput, it is
      // that a UI thread never spends a second deriving a key.
      //
      // The operations below share mutable state (the progressive passkey stack,
      // the file itself), so they are not meant to overlap: await one before
      // starting the next. Their events are raised from the worker thread.

      public Task<IUser?> LoginAsync(string passkey, CancellationToken cancellationToken = default)
         => Task.Run(() => Login(passkey), cancellationToken);

      public Task SaveAsync(CancellationToken cancellationToken = default)
         => Task.Run(Save, cancellationToken);

      public Task<bool> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
         => Task.Run(() => ImportFromFile(filePath), cancellationToken);

      public Task<bool> ExportToFileAsync(string filePath, CancellationToken cancellationToken = default)
         => Task.Run(() => ExportToFile(filePath), cancellationToken);

      #endregion

      internal User? User { get; private set; }
      internal AutoSave AutoSave { get; private set; }
      internal ActivityCenter ActivityCenter { get; private set; }
      internal IEnumerable<Warning>? Warnings { get; private set; }

      internal string Username { get; private set; }
      internal string[] Passkeys { get; private set; }

      internal readonly string HeaderFileEntry = "header";
      internal readonly string DatabaseFileEntry = "database";
      internal readonly string AutoSaveFileEntry = "autosave";
      internal readonly string ActivityFileEntry = "activity";
      internal FileLocker FileLocker { get; private set; }

      // The key-derivation parameters governing how this file's passkeys are
      // stretched, including its random per-database salt. Taken from the crypto
      // center when the database is created (which mints the salt), then read
      // back from the header whenever it is reopened. A file keeps the parameters
      // and salt it was created with.
      private readonly KdfParameters _slowHashParameters;

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

         AutoSave = new()
         {
            Database = this,
         };

         FileLocker = new(cryptographicCenter, serializationCenter, databaseFile, fileMode);

         // New databases adopt the crypto center's current parameters; existing
         // ones are read from the versioned header they were written with.
         _slowHashParameters = fileMode == FileMode.Create
            ? CryptographyCenter.DefaultSlowHashParameters
            : FileLocker.Open<KdfParameters>(HeaderFileEntry);

         Passkeys = [CryptographyCenter.GetHash(username)];

         if (passkeys is not null)
         {
            Passkeys = [.. Passkeys, .. passkeys.Select(x => CryptographyCenter.GetSlowHash(x, _slowHashParameters))];
         }

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
         ArgumentNullException.ThrowIfNull(cryptographicCenter);
         ArgumentNullException.ThrowIfNull(serializationCenter);
         ArgumentNullException.ThrowIfNull(passwordFactory);
         ArgumentNullException.ThrowIfNull(clipboardManager);
         ArgumentNullException.ThrowIfNull(passkeys);

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
            PrivateKey = ProtectedSecret.Protect(privateKey),
            ItemId = "U" + cryptographicCenter.GetHash(username),
            Username = username,
            Passkeys = [.. passkeys.Select(ProtectedSecret.Protect)],
         };

         database.ActivityCenter.AddActivity(itemId: string.Empty,
            eventType: ActivityEventType.DatabaseCreated,
            data: [username],
            needsReview: false);

         database._save(logSaveEvent: false);

         return database;
      }

      /// <summary>
      /// Same as <see cref="Create"/>, but handed to a worker thread so the
      /// caller stays responsive. Creating a database is the single most
      /// expensive operation of the whole application: an RSA-4096 key pair, one
      /// stretching per passkey, then a full save.
      /// </summary>
      public static Task<IDatabase> CreateAsync(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username,
         string[] passkeys,
         CancellationToken cancellationToken = default)
         => Task.Run(() => Create(cryptographicCenter,
               serializationCenter,
               passwordFactory,
               clipboardManager,
               databaseFile,
               username,
               passkeys),
            cancellationToken);

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

         database.ActivityCenter.AddActivity(itemId: string.Empty,
            eventType: ActivityEventType.DatabaseOpened,
            data: [username],
            needsReview: false);

         return database;
      }

      /// <summary>
      /// Same as <see cref="Open"/>, but handed to a worker thread so the caller
      /// stays responsive. Opening reads and decrypts the whole activity log,
      /// which grows with the file's history.
      /// </summary>
      public static Task<IDatabase> OpenAsync(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username,
         CancellationToken cancellationToken = default)
         => Task.Run(() => Open(cryptographicCenter,
               serializationCenter,
               passwordFactory,
               clipboardManager,
               databaseFile,
               username),
            cancellationToken);

      internal T Get<T>(T value)
      {
         User?.ResetTimer();

         return value;
      }

      private void _save(bool logSaveEvent)
      {
         _saveActivities(rebuildStringActivities: true);
         _saveDatabase(logSaveEvent);
      }

      private void _saveDatabase(bool logSaveEvent)
      {
         if (User is null) throw new NullValueException(nameof(User));

         Username = User.Username;

         // Record the file's stretching parameters in its (unencrypted) header so
         // the database entry written just below can always be reopened with the
         // exact parameters it was encrypted with.
         FileLocker.Save(_slowHashParameters, HeaderFileEntry);

         // Anchor the activity log's seal inside the (tamper-proof) database so a
         // later rollback or signature strip of the log becomes detectable. The
         // activities were just (re)sealed by the _saveActivities call above.
         User.ActivitySealWatermark = ActivityCenter.SealedCount;

         // Re-stretching every passkey on each Save is the most expensive step of
         // a save (PBKDF2 × N). Skip it when neither the username nor the master
         // passkeys changed; the session already holds the derived key material.
         if (User.CredentialChanged)
         {
            Passkeys = [CryptographyCenter.GetHash(User.Username), .. User.Passkeys.Select(x => CryptographyCenter.GetSlowHash(x.Reveal(), _slowHashParameters))];
            User.CredentialChanged = false;
         }

         FileLocker.Save(User, DatabaseFileEntry, Passkeys);

         if (logSaveEvent)
         {
            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseSaved,
               data: [Username],
               needsReview: false);
         }

         AutoSave.Clear(deleteFile: true);

         // DatabaseSaved (and any earlier debounced item events) must hit disk
         // before Save returns, matching the previous durability guarantee.
         ActivityCenter.Flush();

         _ = Task.Run(_lookAtWarningsAsync);

         User.ResetTimer();

         DatabaseSaved?.Invoke(this, EventArgs.Empty);
      }

      private void _saveActivities(bool rebuildStringActivities)
      {
         if (User is null) throw new NullValueException(nameof(User));

         ActivityCenter.Username = User.Username;
         ActivityCenter.Save(rebuildStringActivities);
      }

      internal void Close(bool logCloseEvent, bool loginTimeoutReached)
      {
         if (logCloseEvent)
         {
            if (User is not null)
            {
               bool needsReview = AutoSave.Any();

               if (needsReview)
               {
                  // Debounced edits may not have reached the ZIP yet; flush so
                  // the recovery file is present for the next Open.
                  AutoSave.Flush();
               }
               else
               {
                  AutoSave.Clear(deleteFile: true);
               }

               ActivityCenter.AddActivity(itemId: string.Empty,
                  eventType: ActivityEventType.UserLoggedOut,
                  data: [Username, needsReview ? "1" : string.Empty],
                  needsReview);
            }

            ActivityCenter.AddActivity(itemId: string.Empty,
               eventType: ActivityEventType.DatabaseClosed,
               data: [Username],
               needsReview: false);

            // Seal + write while the private key is still available. Must run
            // before User is cleared below.
            ActivityCenter.Flush();
         }
         else
         {
            AutoSave.Clear(deleteFile: false);
            ActivityCenter.CancelPending();
         }

         // Stop the session timer before tearing down the file handle: this both
         // blocks until any in-flight tick finishes and prevents future ticks
         // from operating on the disposed FileLocker.
         User?.StopTimer();

         AutoSave.Dispose();
         ActivityCenter.Dispose();

         User = null;
         Username = string.Empty;
         Passkeys = [];
         Warnings = null;

         FileLocker.Dispose();

         DatabaseClosed?.Invoke(this, new(loginTimeoutReached));
      }

      private void _handleAutoSave(AutoSaveMergeBehavior mergeAutoSave)
      {
         if (User is null) throw new NullValueException(nameof(User));

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
               _saveActivities(rebuildStringActivities: false);
               break;
            case AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile:
               AutoSave.Clear(deleteFile: true);
               break;
            case AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile:
            default:
               break;
         }

         ActivityCenter.AddActivity(itemId: string.Empty,
            eventType: _toActivityEventType(mergeAutoSave),
            data: [Username],
            needsReview: true);
      }

      // Maps an auto-save handling outcome to the activity event that records it.
      // The two enums are deliberately independent: this explicit switch replaces
      // a brittle numeric cast that relied on their values coinciding, so
      // reordering either enum can no longer silently log the wrong event. A new
      // AutoSaveMergeBehavior value now forces a compile-time review here.
      private static ActivityEventType _toActivityEventType(AutoSaveMergeBehavior mergeBehavior) => mergeBehavior switch
      {
         AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile => ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile,
         AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile => ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile,
         AutoSaveMergeBehavior.DontMergeAndRemoveAutoSaveFile => ActivityEventType.DontMergeAndRemoveAutoSaveFile,
         AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile => ActivityEventType.DontMergeAndKeepAutoSaveFile,
         _ => ActivityEventType.None,
      };

      private async Task _lookAtWarningsAsync()
      {
         if (User is null) return;

         try
         {
            Warning[] activityWarnings = _lookAtActivityWarnings();
            Warning[] passwordUpdateReminderWarnings = _lookAtPasswordUpdateReminderWarnings();
            Warning[] passwordLeakedWarnings = await _lookAtPasswordLeakedWarningsAsync().ConfigureAwait(false);
            Warning[] duplicatedPasswordsWarnings = _lookAtDuplicatedPasswordsWarnings();

            Warnings = [..activityWarnings,
               ..passwordUpdateReminderWarnings,
               ..passwordLeakedWarnings,
               ..duplicatedPasswordsWarnings];

            // The leak check awaits a remote service, so the session may have
            // been closed in the meantime: notify against the user observed now,
            // not the one observed when the scan started.
            User? user = User;

            if (user is null) return;

            WarningsUpdated?.Invoke(this, new WarningsUpdatedEventArgs([.. Warnings.Where(x => user.WarningsToNotify.HasFlag(x.WarningType))]));
         }
#pragma warning disable CA1031 // Last-resort barrier: the background warning scan must never crash the session
         catch (Exception ex)
#pragma warning restore CA1031
         {
            // The warning scan runs on a background task and must never crash the
            // session; a failure only means warnings are not refreshed this round,
            // so we trace it for diagnostics rather than swallowing it silently.
            System.Diagnostics.Trace.TraceWarning($"Warning scan failed: {ex}");
         }
      }

      private Warning[] _lookAtActivityWarnings()
      {
         if (User is null) throw new NullValueException(nameof(User));
         if (ActivityCenter.Activities is null) throw new NullValueException(nameof(ActivityCenter.Activities));

         IActivity[] activities = [.. ActivityCenter.Activities.Where(x => x.NeedsReview)];

         return activities.Length != 0 ? [new Warning([.. activities])] : [];
      }

      private Warning[] _lookAtPasswordUpdateReminderWarnings()
      {
         if (User is null) return [];

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.PasswordExpired)];

         return accounts.Length != 0 ? [new Warning(WarningType.PasswordUpdateReminderWarning, accounts)] : [];
      }

      // Leak checks are the only outbound calls the application makes, and the
      // previous parallel fan-out fired one request - and blocked one thread -
      // per distinct password at once. Requests are now awaited rather than
      // blocking, and issued in bounded batches so a large database cannot flood
      // a courtesy service.
      private const int MAX_CONCURRENT_LEAK_CHECKS = 8;

      private async Task<Warning[]> _lookAtPasswordLeakedWarningsAsync()
      {
         if (User is null) return [];

         string[] passwordsToCheck = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.Options.HasFlag(AccountOption.WarnIfPasswordLeaked))
            .Select(x => x.Password)
            .Distinct()];

         HashSet<string> leakedPasswords = [];

         foreach (string[] batch in passwordsToCheck.Chunk(MAX_CONCURRENT_LEAK_CHECKS))
         {
            bool[] leaked = await Task.WhenAll(batch.Select(x => PasswordFactory.PasswordLeakedAsync(x))).ConfigureAwait(false);

            for (int i = 0; i < batch.Length; i++)
            {
               if (leaked[i])
               {
                  _ = leakedPasswords.Add(batch[i]);
               }
            }
         }

         if (User is null) return [];

         Account[] accounts = [.. User.Services
            .SelectMany(x => x.Accounts)
            .Where(x => x.Options.HasFlag(AccountOption.WarnIfPasswordLeaked)
               && leakedPasswords.Contains(x.Password))];

         foreach (Account account in accounts)
         {
            account.PasswordLeaked = true;
         }

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
            if (accounts.Any(x => x.Options.HasFlag(AccountOption.WarnIfDuplicatedPassword)))
            {
               warnings.Add(new(WarningType.DuplicatedPasswordsWarning, [.. accounts.Cast<Account>()]));
            }
         }

         return [.. warnings];
      }
   }
}
