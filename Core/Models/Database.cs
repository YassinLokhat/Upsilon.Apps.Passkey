using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.Core.Models
{
   /// <summary>
   /// Vault implementation: a ZIP <c>.pku</c> with onion-encrypted user data,
   /// an autosave entry, a sealed activity log, and a sticky KDF header.
   /// Create with <see cref="Create"/> / <see cref="Open"/>; do not construct
   /// directly. After <see cref="Create"/> the user is already logged in —
   /// do not call <see cref="Login"/> again on that instance.
   /// </summary>
   public sealed partial class Database : IDatabase
   {
      public string DatabaseFile { get; set; }

      IUser? IDatabase.User => User;
      int? IDatabase.SessionLeftTime => User?.SessionLeftTime;

      IEnumerable<IActivity>? IDatabase.Activities => Get(ActivityCenter.GetActivitiesOrdered());

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
         if (User is null)
         {
            throw new NullValueException(nameof(User));
         }

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
         catch (IncompleteOnionException)
         {
            // More passkeys still required — not a LoginFailed.
         }
         // CorruptedSourceException and other failures propagate to the GUI.

         if (User is not null)
         {
            User.Host = this;

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
               AutoSave.Host = this;

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

      internal User? User { get; private set; }
      internal AutoSave AutoSave { get; private set; }
      internal ActivityCenter ActivityCenter { get; private set; }
      internal IEnumerable<Warning>? Warnings { get; private set; }

      internal string Username { get; private set; }
      internal string[] Passkeys { get; private set; }

      // ZIP entry names inside a .pku. The header is unencrypted (KDF params);
      // database and autosave are onion-encrypted; activity records are RSA-hybrid.
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
         IEnumerable<string>? passkeys = null)
      {
         DatabaseFile = databaseFile;

         CryptographyCenter = cryptographicCenter;
         SerializationCenter = serializationCenter;
         PasswordFactory = passwordFactory;
         ClipboardManager = clipboardManager;

         Username = username;

         AutoSave = new()
         {
            Host = this,
         };

         FileLocker = new(cryptographicCenter, serializationCenter, databaseFile, fileMode);

         // New databases adopt the crypto center's current parameters; opened
         // databases read the salt and work factor from the header entry.
         _slowHashParameters = fileMode == FileMode.Create
            ? CryptographyCenter.DefaultSlowHashParameters
            : FileLocker.Open<KdfParameters>(HeaderFileEntry);

         try
         {
            CryptographyCenter.EnsureSufficientSlowHashParameters(_slowHashParameters);
         }
         catch
         {
            // Constructor failed after taking the file lock: release it so the
            // caller can retry or inspect the .pku without a sharing violation.
            FileLocker.Dispose();
            throw;
         }

         Passkeys = [CryptographyCenter.GetHash(username)];

         if (passkeys is not null)
         {
            Passkeys = [.. Passkeys, .. passkeys.Select(x => CryptographyCenter.GetSlowHash(x, _slowHashParameters))];
         }

         ActivityCenter = fileMode == FileMode.Create
            ? new()
            {
               PublicKey = publicKey,
            }
            : FileLocker.Open<ActivityCenter>(ActivityFileEntry);

         ActivityCenter.Host = this;
      }

      /// <summary>
      /// Creates a new <c>.pku</c> file, mints an RSA-4096 key pair, stretches
      /// every passkey, writes the vault, and returns an already-logged-in
      /// database. <paramref name="databaseFile"/> must not already exist.
      /// Prefer <see cref="CreateAsync"/> from a UI thread.
      /// </summary>
      public static IDatabase Create(ICryptographyCenter cryptographicCenter,
         ISerializationCenter serializationCenter,
         IPasswordFactory passwordFactory,
         IClipboardManager clipboardManager,
         string databaseFile,
         string username,
         IEnumerable<string> passkeys)
      {
         ArgumentNullException.ThrowIfNull(cryptographicCenter);
         ArgumentNullException.ThrowIfNull(serializationCenter);
         ArgumentNullException.ThrowIfNull(passwordFactory);
         ArgumentNullException.ThrowIfNull(clipboardManager);
         ArgumentNullException.ThrowIfNull(passkeys);

         // Snapshot once: Create may receive a one-shot sequence, and the
         // constructor plus User.Passkeys both need the same ordered values.
         string[] passkeyList = [.. passkeys];

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
            passkeyList);

         database.User = new()
         {
            Host = database,
            PrivateKey = ProtectedSecret.Protect(privateKey),
            ItemId = "U" + cryptographicCenter.GetHash(username),
            Username = username,
            Passkeys = [.. passkeyList.Select(ProtectedSecret.Protect)],
         };

         database.ActivityCenter.AddActivity(itemId: string.Empty,
            eventType: ActivityEventType.DatabaseCreated,
            data: [username],
            needsReview: false);

         database._save(logSaveEvent: false);

         return database;
      }

      /// <summary>
      /// Opens an existing <c>.pku</c>. <see cref="IDatabase.User"/> stays
      /// <see langword="null"/> until progressive <see cref="Login"/> succeeds
      /// with every passkey, in order. Prefer <see cref="OpenAsync"/> from a UI thread.
      /// </summary>
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
      /// Pass-through used by explicit interface getters. Touching any vault
      /// field also resets the inactivity timer, so a read counts as activity.
      /// </summary>
      internal T Get<T>(T value)
      {
         User?.ResetTimer();

         return value;
      }
   }
}
