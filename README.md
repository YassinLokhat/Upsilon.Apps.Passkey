**Upsilon.Apps.Passkey**
=============================================

**Overview**
------------

A local-only password manager written in C# on **.NET 10**. There is no server,
no account, and no synchronization: every secret lives in a single encrypted
`.pku` file on the user's device. Version <!-- BEGIN:versions-overview -->**2.0.0**<!-- END:versions-overview --> (each assembly is versioned
independently; see [SECURITY.md](SECURITY.md) and [`versions.json`](versions.json)).

**Features**
------------

*   **Password storage**: services, accounts, identifiers, notes, and password history
*   **Multi-passkey vault**: ordered master passkeys form an AES-256-GCM onion (see [SECURITY.md](SECURITY.md))
*   **Activity log**: tamper-evident audit trail of vault events
*   **Alerts**: activity review, password reminders / duplicates / leaks, vault & host security posture, passkey quality (count, strength, leak, reuse)
*   **Autosave**: unsaved edits are kept in the `.pku` ZIP and merged on the next login
*   **Undo / Redo**: session-scoped edit history (Ctrl+Shift+Z / Ctrl+Shift+Y in the WPF vault window); cleared on logout
*   **Password generation**: CSPRNG over a configurable alphabet
*   **Leak detection**: opt-in Have I Been Pwned checks, then XposedOrNot failover, then an optional local HIBP Bloom filter (k-anonymity / offline; see [SECURITY.md](SECURITY.md))
*   **Import / Export**: plaintext JSON (settings + services) or CSV (services only; import accepts comma- or tab-delimited)
*   **WPF client** (Windows): System / Light / Dark theme, QR codes, global paste hotkeys, auto-logout, clipboard cleaning

**Architecture**
----------------

Four layers, two solution files:

```
Interfaces/     Public contracts (IDatabase, IUser, crypto, clipboard, IProtectedSecret, ISecretMemoryProtector, …)
Utils/          Default crypto, JSON, password factory, SecretMemoryProtector / ProtectedSecret, LeakFilter (.pkbf). Zero NuGet (BCL only).
Core/           Vault implementation (Interfaces only — no ProjectReference to Utils). Zero NuGet packages (BCL only).
GUI/WPF/        Windows desktop client (MVVM + a small AppServices locator); composes Utils defaults.
UnitTests/      Multiplateform (net10.0: Core/Utils) + Windows (net10.0-windows: WPF ViewModels).
```

| Solution | Projects |
| -------- | -------- |
| `Upsilon.Apps.Passkey.Windows.slnx` | Interfaces, Utils, Core, WPF GUI, both test projects |
| `Upsilon.Apps.Passkey.Linux.slnx` | Interfaces, Utils, Core, Multiplateform tests |

Core talks to the OS for clipboard only through an injected port
(`IClipboardManager` must be OS-specific). In-memory secret wrapping goes through
`ISecretMemoryProtector` (Utils ships `SecretMemoryProtector`). File I/O uses the BCL in Core.
Opt-in HTTP leak checks and the optional offline HIBP Bloom filter live in Utils
(`PasswordFactory`, `Utils/LeakFilter/`). The WPF app supplies
the clipboard implementation, the secret protector, and hosts dialogs, session, and navigation behind
`AppServices` so ViewModels stay testable without a window.

**Security**
------------

*   **At rest**: AES-256-GCM onion (HKDF-SHA256 per layer) over ordered passkeys; the activity log uses RSA-4096 hybrid encryption plus a login-time seal. See [SECURITY.md](SECURITY.md).
*   **In memory**: account passwords, passkeys, and the RSA private key are held as `IProtectedSecret` via an injected `ISecretMemoryProtector` (default: Utils `ProtectedSecret`, process-wide AES-GCM) and only revealed just in time.
*   **Session**: configurable auto-logout, clipboard auto-clear (including Windows clipboard history), and progressive login without rollback.
*   **Supply chain**: Core, Utils, Interfaces, and the WPF GUI refuse any third-party NuGet package at build time. GitHub CodeQL scans production code on CI.

**Models**
----------

### Class diagram
```mermaid
classDiagram
  direction LR
  class EventArgs
  class IReadOnlyList
  class JsonConverter
  class IDisposable
  class EventHandler
  class IEquatable
  class Exception
  namespace Upsilon.Apps.Passkey.Interfaces.Enums {
    class AccountOption {
        <<Flags>>
        None
        WarnIfPasswordLeaked
        WarnIfDuplicatedPassword
        WarnIfWeakPassword
    }
    class ActivityEventType {
        None
        MergeAndSaveThenRemoveAutoSaveFile
        MergeWithoutSavingAndKeepAutoSaveFile
        DontMergeAndRemoveAutoSaveFile
        DontMergeAndKeepAutoSaveFile
        DatabaseCreated
        DatabaseOpened
        DatabaseSaved
        DatabaseClosed
        LoginSessionTimeoutReached
        LoginFailed
        UserLoggedIn
        UserLoggedOut
        ImportingDataStarted
        ImportingDataSucceeded
        ImportingDataFailed
        ExportingDataStarted
        ExportingDataSucceeded
        ExportingDataFailed
        ItemUpdated
        ItemAdded
        ItemDeleted
        ActivityLogTampered
    }
    class AlertSeverity {
        Info
        Warning
        Critical
    }
    class AutoSaveMergeBehavior {
        Undefined
        MergeAndSaveThenRemoveAutoSaveFile
        MergeWithoutSavingAndKeepAutoSaveFile
        DontMergeAndRemoveAutoSaveFile
        DontMergeAndKeepAutoSaveFile
    }
    class HostSecurityIssue {
        <<Flags>>
        None
        IdleLoginDisabled
        OfflineLeakFilterUnavailable
    }
    class IdentifierType {
        Username
        Email
        PhoneNumber
        Passkey
        AuthenticatorApp
    }
    class ImportExportError {
        None
        ImportFileNotAccessible
        ImportFileTooLarge
        ExtensionFileNotSupported
        CSVHeadersDontMatch
        IncorrectCSVFormat
        NoDataToImport
        ImportFileDeserializationFailed
        ServiceAlreadyExists
        BlankService
        ExportFileAlreadyExists
        SecurityTimeoutsDisabled
    }
    class KdfAlgorithm {
        Pbkdf2HmacSha256
        Pbkdf2HmacSha512
    }
    class SecretQualityIssue {
        <<Flags>>
        None
        TooShort
        LowDiversity
        MatchesUsername
        TrivialPattern
    }
    class SecuritySettingsIssue {
        <<Flags>>
        None
        AutoLogoutDisabled
        ClipboardCleaningDisabled
        QrAutoCloseDisabled
        NoAccountLeakCheck
        NoAccountDuplicateCheck
        NoAccountUpdateReminder
        NoAccountWeakPasswordCheck
    }
  }
  namespace Upsilon.Apps.Passkey.Interfaces.Events {
    class AlertsChangedEventArgs {
        + Kind : string
        + Alerts : IReadOnlyList~IAlert~
    }
    class AutoSaveDetectedEventArgs {
        + MergeBehavior : AutoSaveMergeBehavior
    }
    class LogoutEventArgs {
        + LoginTimeoutReached : bool
    }
  }
  namespace Upsilon.Apps.Passkey.Interfaces.Models {
    class AlertKindList {
        + Default : AlertKindList$
        + Count : int
        + this int : string
        + GetEnumerator() IEnumerator~string~
        + ToArray() string[]
        + Contains(kind : string) bool
        + ToString() string
    }
    class AlertKindListJsonConverter {
        + Read(reader : Utf8JsonReader, typeToConvert : Type, options : JsonSerializerOptions) AlertKindList
        + Write(writer : Utf8JsonWriter, value : AlertKindList, options : JsonSerializerOptions) void
    }
    class AlertKinds {
        + SourceCore : string
        + SourceHost : string
        + ActivityReview : string
        + PasswordUpdateReminder : string
        + DuplicatedPasswords : string
        + PasswordLeaked : string
        + VaultSecuritySettings : string
        + InsufficientPasskeys : string
        + WeakPasskey : string
        + PasskeyLeaked : string
        + WeakAccountPassword : string
        + PasskeyReusedAsAccountPassword : string
        + HostSecuritySettings : string
        + RecommendedPasskeyCount : int
        + AllCore : string[]$
        + AllHost : string[]$
        + DefaultNotify : string[]$
    }
    class IAccount {
        + Service : IService
        + Label : string
        + Notes : string
        + Identifiers : IEnumerable~IIdentifier~
        + Password : string
        + Passwords : Dictionary~DateTime, string~
        + PasswordUpdateReminderDelay : int
        + Options : AccountOption
    }
    class IAccountsAlert {
        + Accounts : IEnumerable~IAccount~
    }
    class IPasswordUpdateReminderAlert
    class IDuplicatedPasswordsAlert
    class IPasswordLeakedAlert
    class IWeakAccountPasswordAlert
    class IPasskeyReuseAlert
    class IActivity {
        + DateTime : DateTime
        + ItemId : string
        + Username : string?
        + ServiceName : string?
        + AccountName : string?
        + FieldName : string?
        + FieldValue : string?
        + ParentName : string?
        + EventType : ActivityEventType
        + NeedsReview : bool
    }
    class IActivityReviewAlert {
        + Activities : IEnumerable~IActivity~
    }
    class IAlert {
        + Source : string
        + Kind : string
        + Severity : AlertSeverity
    }
    class IDatabase {
        + DatabaseFile : string
        + User : IUser?
        + EditHistory : IEditHistory
        + SessionLeftTime : int?
        + Activities : IEnumerable~IActivity~?
        + SerializationCenter : ISerializationCenter
        + CryptographyCenter : ICryptographyCenter
        + PasswordFactory : IPasswordFactory
        + ClipboardManager : IClipboardManager
        + SecretMemoryProtector : ISecretMemoryProtector
        + CoreAlerts : IReadOnlyDictionary~string, IReadOnlyList~IAlert~~
        + CoreAlertsChanged : EventHandler~AlertsChangedEventArgs~?
        + CoreAlertsScanCompleted : EventHandler?
        + AutoSaveDetected : EventHandler~AutoSaveDetectedEventArgs~?
        + DatabaseSaved : EventHandler?
        + DatabaseClosed : EventHandler~LogoutEventArgs~?
        + Login(passkey : string)* IUser?
        + LoginAsync(passkey : string, cancellationToken : CancellationToken)* Task~IUser?~
        + Save()* void
        + SaveAsync(cancellationToken : CancellationToken)* Task
        + RefreshAlerts()* void
        + Delete()* void
        + Close()* void
        + HasChanged(itemId : string)* bool
        + HasChanged(itemId : string, fieldName : string)* bool
        + ImportFromFile(filePath : string)* ImportExportError
        + ImportFromFileAsync(filePath : string, cancellationToken : CancellationToken)* Task~ImportExportError~
        + ExportToFile(filePath : string)* ImportExportError
        + ExportToFileAsync(filePath : string, cancellationToken : CancellationToken)* Task~ImportExportError~
    }
    class IEditHistory {
        + CanUndo : bool
        + CanRedo : bool
        + HistoryChanged : EventHandler?
        + Undo()* void
        + Redo()* void
        + Clear()* void
    }
    class IHostSecuritySettingsAlert {
        + Issues : HostSecurityIssue
    }
    class IIdentifier {
        + Type : IdentifierType
        + Value : string
    }
    class IItem {
        + ItemId : string
        + Database : IDatabase
        + HasChanged()* bool
    }
    class IInsufficientPasskeysAlert {
        + Count : int
        + RecommendedMinimum : int
    }
    class IWeakPasskeyAlert {
        + PasskeyIndexes : IReadOnlyList~int~
        + Issues : SecretQualityIssue
    }
    class IPasskeyLeakedAlert {
        + PasskeyIndexes : IReadOnlyList~int~
    }
    class IService {
        + User : IUser
        + ServiceName : string
        + Url : Uri?
        + Notes : string
        + Accounts : IEnumerable~IAccount~
        + AddAccount(label : string, identifiers : IEnumerable~IIdentifier~, password : string)* IAccount
        + AddAccount(label : string, identifiers : IEnumerable~IIdentifier~)* IAccount
        + AddAccount(identifiers : IEnumerable~IIdentifier~, password : string)* IAccount
        + AddAccount(identifiers : IEnumerable~IIdentifier~)* IAccount
        + DeleteAccount(account : IAccount)* void
    }
    class ISettings {
        + LogoutTimeout : int
        + CleaningClipboardTimeout : int
        + ShowPasswordDelay : int
        + NumberOfOldPasswordToKeep : int
        + NumberOfMonthActivitiesToKeep : int
        + AlertsToNotify : AlertKindList
        + FollowAppCode string$
        + Language : string
        + Theme : string
    }
    class IUser {
        + Username : string
        + Passkeys : IEnumerable~string~
        + Settings : ISettings
        + Services : IEnumerable~IService~
        + AddService(serviceName : string)* IService
        + DeleteService(service : IService)* void
        + RememberClipboardSecret(text : string)* void
    }
    class IVaultSecuritySettingsAlert {
        + Issues : SecuritySettingsIssue
    }
    class Identifier {
        + Type : IdentifierType
        + Value : string
        + Equals(other : Identifier?) bool
        + Equals(obj : object?) bool
        + GetHashCode() int
        + ToString() string
    }
  }
  namespace Upsilon.Apps.Passkey.Interfaces.Utils {
    class CorruptedSourceException
    class InsufficientKdfParametersException
    class IncompleteOnionException
    class WrongPasswordException {
        + PasswordLevel : int
    }
    class NullValueException {
        + Name : string
    }
    class IClipboardManager {
        + SetText(text : string, autoClearAfter : TimeSpan?)* void
        + SetText(text : string, autoClearAfter : int)* void
        + RemoveAllOccurrenceAsync(removeList : IEnumerable~string~, cancellationToken : CancellationToken)* Task~int~
    }
    class ICryptographyCenter {
        + DefaultSlowHashParameters : KdfParameters
        + HashLength : int
        + GetHash(source : string)* string
        + GetSlowHash(source : string, parameters : KdfParameters)* string
        + EnsureSufficientSlowHashParameters(parameters : KdfParameters)* void
        + EncryptSymmetrically(source : string, passwords : IEnumerable~string~)* string
        + DecryptSymmetrically(source : string, passwords : IEnumerable~string~)* string
        + GenerateRandomKeys(out publicKey string, out privateKey string)* void
        + EncryptAsymmetrically(source : string, key : string)* string
        + DecryptAsymmetrically(source : string, key : string)* string
        + GetPublicKey(privateKey : string)* string
        + Sign(source : string, privateKey : string)* string
        + Verify(source : string, signature : string, publicKey : string)* bool
    }
    class IPasswordFactory {
        + HasLocalFilter : bool
        + UpperAlphabetic : string
        + LowerAlphabetic : string
        + Numeric : string
        + SpecialChars : string
        + GeneratePassword(length : int, alphabet : string, checkIfLeaked : bool)* string
        + GeneratePasswordAsync(length : int, alphabet : string, checkIfLeaked : bool, cancellationToken : CancellationToken)* Task~string~
        + PasswordLeaked(password : string)* bool
        + PasswordLeakedAsync(password : string, cancellationToken : CancellationToken)* Task~bool~
    }
    class IProtectedSecret {
        + Reveal()* string
    }
    class ISecretMemoryProtector {
        + Protect(secret : string?)* IProtectedSecret
    }
    class ISerializationCenter {
        + Serialize(toSerialize : T)* string
        + Deserialize(toDeserialize : string)* T
    }
    class IdentifierTypeDetector {
        <<static>>
        + Detect(value : string?)$ IdentifierType
    }
    class ItemExtensions {
        <<static>>
        + HasChanged(item : IItem, fieldName : string)$ bool
    }
    class KdfParameters {
        + Algorithm : KdfAlgorithm
        + Iterations : int
        + OutputLength : int
        + Salt : string
    }
    class PlaintextSecret {
        + Wrap(secret : string?)$ IProtectedSecret
        + Reveal() string
        + ToString() string
    }
    class SecretQuality {
        <<static>>
        + MinimumLength : int$
        + MinimumCharacterClasses : int$
        + Evaluate(secret : string, username : string?)$ SecretQualityIssue
        + IsWeak(secret : string, username : string?)$ bool
    }
  }
  <<enumeration>> AccountOption
  <<enumeration>> ActivityEventType
  <<enumeration>> AlertSeverity
  <<enumeration>> AutoSaveMergeBehavior
  <<enumeration>> HostSecurityIssue
  <<enumeration>> IdentifierType
  <<enumeration>> ImportExportError
  <<enumeration>> KdfAlgorithm
  <<enumeration>> SecretQualityIssue
  <<enumeration>> SecuritySettingsIssue
  <<interface>> IAccount
  <<interface>> IAccountsAlert
  <<interface>> IPasswordUpdateReminderAlert
  <<interface>> IDuplicatedPasswordsAlert
  <<interface>> IPasswordLeakedAlert
  <<interface>> IWeakAccountPasswordAlert
  <<interface>> IPasskeyReuseAlert
  <<interface>> IActivity
  <<interface>> IActivityReviewAlert
  <<interface>> IAlert
  <<interface>> IDatabase
  <<interface>> IEditHistory
  <<interface>> IHostSecuritySettingsAlert
  <<interface>> IIdentifier
  <<interface>> IItem
  <<interface>> IInsufficientPasskeysAlert
  <<interface>> IWeakPasskeyAlert
  <<interface>> IPasskeyLeakedAlert
  <<interface>> IService
  <<interface>> ISettings
  <<interface>> IUser
  <<interface>> IVaultSecuritySettingsAlert
  <<interface>> IClipboardManager
  <<interface>> ICryptographyCenter
  <<interface>> IPasswordFactory
  <<interface>> IProtectedSecret
  <<interface>> ISecretMemoryProtector
  <<interface>> ISerializationCenter
  <<external>> EventArgs
  <<external>> IReadOnlyList
  <<external>> JsonConverter
  <<external>> IDisposable
  <<external>> EventHandler
  <<external>> IEquatable
  <<external>> Exception

  style AccountOption fill:#2563eb1f,stroke:#2563eb80
  style ActivityEventType fill:#2563eb1f,stroke:#2563eb80
  style AlertSeverity fill:#2563eb1f,stroke:#2563eb80
  style AutoSaveMergeBehavior fill:#2563eb1f,stroke:#2563eb80
  style HostSecurityIssue fill:#2563eb1f,stroke:#2563eb80
  style IdentifierType fill:#2563eb1f,stroke:#2563eb80
  style ImportExportError fill:#2563eb1f,stroke:#2563eb80
  style KdfAlgorithm fill:#2563eb1f,stroke:#2563eb80
  style SecretQualityIssue fill:#2563eb1f,stroke:#2563eb80
  style SecuritySettingsIssue fill:#2563eb1f,stroke:#2563eb80
  style AlertsChangedEventArgs fill:#16a34a1f,stroke:#16a34a80
  style AutoSaveDetectedEventArgs fill:#16a34a1f,stroke:#16a34a80
  style LogoutEventArgs fill:#16a34a1f,stroke:#16a34a80
  style AlertKindList fill:#d977061f,stroke:#d9770680
  style AlertKindListJsonConverter fill:#d977061f,stroke:#d9770680
  style AlertKinds fill:#d977061f,stroke:#d9770680
  style IAccount fill:#d977061f,stroke:#d9770680
  style IAccountsAlert fill:#d977061f,stroke:#d9770680
  style IPasswordUpdateReminderAlert fill:#d977061f,stroke:#d9770680
  style IDuplicatedPasswordsAlert fill:#d977061f,stroke:#d9770680
  style IPasswordLeakedAlert fill:#d977061f,stroke:#d9770680
  style IWeakAccountPasswordAlert fill:#d977061f,stroke:#d9770680
  style IPasskeyReuseAlert fill:#d977061f,stroke:#d9770680
  style IActivity fill:#d977061f,stroke:#d9770680
  style IActivityReviewAlert fill:#d977061f,stroke:#d9770680
  style IAlert fill:#d977061f,stroke:#d9770680
  style IDatabase fill:#d977061f,stroke:#d9770680
  style IEditHistory fill:#d977061f,stroke:#d9770680
  style IHostSecuritySettingsAlert fill:#d977061f,stroke:#d9770680
  style IIdentifier fill:#d977061f,stroke:#d9770680
  style IItem fill:#d977061f,stroke:#d9770680
  style IInsufficientPasskeysAlert fill:#d977061f,stroke:#d9770680
  style IWeakPasskeyAlert fill:#d977061f,stroke:#d9770680
  style IPasskeyLeakedAlert fill:#d977061f,stroke:#d9770680
  style IService fill:#d977061f,stroke:#d9770680
  style ISettings fill:#d977061f,stroke:#d9770680
  style IUser fill:#d977061f,stroke:#d9770680
  style IVaultSecuritySettingsAlert fill:#d977061f,stroke:#d9770680
  style Identifier fill:#d977061f,stroke:#d9770680
  style CorruptedSourceException fill:#9333ea1f,stroke:#9333ea80
  style InsufficientKdfParametersException fill:#9333ea1f,stroke:#9333ea80
  style IncompleteOnionException fill:#9333ea1f,stroke:#9333ea80
  style WrongPasswordException fill:#9333ea1f,stroke:#9333ea80
  style NullValueException fill:#9333ea1f,stroke:#9333ea80
  style IClipboardManager fill:#9333ea1f,stroke:#9333ea80
  style ICryptographyCenter fill:#9333ea1f,stroke:#9333ea80
  style IPasswordFactory fill:#9333ea1f,stroke:#9333ea80
  style IProtectedSecret fill:#9333ea1f,stroke:#9333ea80
  style ISecretMemoryProtector fill:#9333ea1f,stroke:#9333ea80
  style ISerializationCenter fill:#9333ea1f,stroke:#9333ea80
  style IdentifierTypeDetector fill:#9333ea1f,stroke:#9333ea80
  style ItemExtensions fill:#9333ea1f,stroke:#9333ea80
  style KdfParameters fill:#9333ea1f,stroke:#9333ea80
  style PlaintextSecret fill:#9333ea1f,stroke:#9333ea80
  style SecretQuality fill:#9333ea1f,stroke:#9333ea80

  EventArgs <|-- AlertsChangedEventArgs
  AlertsChangedEventArgs o--> "*" IAlert : Alerts
  EventArgs <|-- AutoSaveDetectedEventArgs
  AutoSaveDetectedEventArgs *--> "1" AutoSaveMergeBehavior : MergeBehavior
  EventArgs <|-- LogoutEventArgs
  IReadOnlyList <|-- AlertKindList
  JsonConverter <|-- AlertKindListJsonConverter
  IItem <|.. IAccount
  IAccount o--> "1" IService : Service
  IAccount o--> "*" IIdentifier : Identifiers
  IAccount *--> "1" AccountOption : Options
  IAlert <|.. IAccountsAlert
  IAccountsAlert o--> "*" IAccount : Accounts
  IAccountsAlert <|.. IPasswordUpdateReminderAlert
  IAccountsAlert <|.. IDuplicatedPasswordsAlert
  IAccountsAlert <|.. IPasswordLeakedAlert
  IAccountsAlert <|.. IWeakAccountPasswordAlert
  IAccountsAlert <|.. IPasskeyReuseAlert
  IActivity *--> "1" ActivityEventType : EventType
  IAlert <|.. IActivityReviewAlert
  IActivityReviewAlert o--> "*" IActivity : Activities
  IAlert *--> "1" AlertSeverity : Severity
  IDisposable <|-- IDatabase
  IDatabase o--> "0..1" IUser : User
  IDatabase o--> "1" IEditHistory : EditHistory
  IDatabase o--> "*" IActivity : Activities
  IDatabase o--> "1" ISerializationCenter : SerializationCenter
  IDatabase o--> "1" ICryptographyCenter : CryptographyCenter
  IDatabase o--> "1" IPasswordFactory : PasswordFactory
  IDatabase o--> "1" IClipboardManager : ClipboardManager
  IDatabase o--> "1" ISecretMemoryProtector : SecretMemoryProtector
  IDatabase o--> "*" IAlert : CoreAlerts
  IDatabase ..> AlertsChangedEventArgs : CoreAlertsChanged
  IDatabase ..> AutoSaveDetectedEventArgs : AutoSaveDetected
  IDatabase ..> LogoutEventArgs : DatabaseClosed
  IAlert <|.. IHostSecuritySettingsAlert
  IHostSecuritySettingsAlert *--> "1" HostSecurityIssue : Issues
  IIdentifier *--> "1" IdentifierType : Type
  IItem o--> "1" IDatabase : Database
  IAlert <|.. IInsufficientPasskeysAlert
  IAlert <|.. IWeakPasskeyAlert
  IWeakPasskeyAlert *--> "1" SecretQualityIssue : Issues
  IAlert <|.. IPasskeyLeakedAlert
  IItem <|.. IService
  IService o--> "1" IUser : User
  IService o--> "*" IAccount : Accounts
  ISettings o--> "1" AlertKindList : AlertsToNotify
  IItem <|.. IUser
  IUser o--> "1" ISettings : Settings
  IUser o--> "*" IService : Services
  IAlert <|.. IVaultSecuritySettingsAlert
  IVaultSecuritySettingsAlert *--> "1" SecuritySettingsIssue : Issues
  IIdentifier <|.. Identifier
  IEquatable <|-- Identifier
  Identifier *--> "1" IdentifierType : Type
  Exception <|-- CorruptedSourceException
  Exception <|-- InsufficientKdfParametersException
  Exception <|-- IncompleteOnionException
  Exception <|-- WrongPasswordException
  Exception <|-- NullValueException
  ICryptographyCenter o--> "1" KdfParameters : DefaultSlowHashParameters
  KdfParameters *--> "1" KdfAlgorithm : Algorithm
  IProtectedSecret <|.. PlaintextSecret
  AlertKindListJsonConverter ..> AlertKindList : Read
  IDatabase ..> ImportExportError : ImportFromFile
  IService ..> IIdentifier : AddAccount
  ISecretMemoryProtector ..> IProtectedSecret : Protect
  IdentifierTypeDetector ..> IdentifierType : Detect
  ItemExtensions ..> IItem : HasChanged
  SecretQuality ..> SecretQualityIssue : Evaluate
```

**Example Use Cases**

--------------------

### Create a new database

To create a new database, use the `Upsilon.Apps.Passkey.Core.Models.Database.Create` static method.

This method needs an `ICryptographyCenter` implementation, an `ISerializationCenter` implementation, an `IPasswordFactory` implementation, an `IClipboardManager` implementation, and an `ISecretMemoryProtector` implementation.
The namespace `Upsilon.Apps.Passkey.Utils` already contains implementations for all of these interfaces except for the `IClipboardManager` which needs an OS specific implementation.

The next parameter is the database file itself, which will be created during the process.

Finally, the method take the username and the passkeys.
Note that the passkeys are used as master passwords to encrypt the database (and the other files).

```csharp
IDatabase database = Upsilon.Apps.Passkey.Core.Models.Database.Create(new Upsilon.Apps.Passkey.Utils.CryptographyCenter(),
   new Upsilon.Apps.Passkey.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.Passkey.Utils.PasswordFactory(),
   new OSSpecificClipboardManager(),
   new Upsilon.Apps.Passkey.Utils.SecretMemoryProtector(),
   "./database.pku",
   "username",
   new[] { "master_password_1", "master_password_2", "master_password_3" });
```

`CreateAsync` is the same work on a worker thread (RSA-4096 keygen plus one
PBKDF2 stretch per passkey). Prefer it from a UI.

After creation, the method opens the database **and logs the user in**:
`database.User` is already set. Do **not** call `Login` afterwards — that would
append another onion layer on top of an already-complete stack and fail. Progressive
`Login` is only needed after `Open` (see the next use cases).

```csharp
IUser user = database.User!;	// Already logged in after Create
```

### Open an existing database

To open an existing database, use the `Upsilon.Apps.Passkey.Core.Models.Database.Open` static method.

This method needs an `ICryptographyCenter` implementation, an `ISerializationCenter` implementation, an `IPasswordFactory` implementation, an `IClipboardManager` implementation, and an `ISecretMemoryProtector` implementation as in the creation step.

The next parameter is the database file itself and must, obviously, exist.

Finally, the method take the username.

```csharp
IDatabase database = Upsilon.Apps.Passkey.Core.Models.Database.Open(new Upsilon.Apps.Passkey.Utils.CryptographyCenter(),
   new Upsilon.Apps.Passkey.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.Passkey.Utils.PasswordFactory(),
   new OSSpecificClipboardManager(),
   new Upsilon.Apps.Passkey.Utils.SecretMemoryProtector(),
   "./database.pku",
   "username");
```

After `Open`, `database.User` is still `null` until progressive login succeeds.

### Login to an user

After opening a database, use the `IDatabase.Login` method to login the user.
To do that, call the login method with every passkeys used during the database creation process.
Only the last call of that method, with every correct and ordered passkeys, will return the `IUser` representing the current user successfully logged in.
Else that method will return `null`.

```csharp
IUser? user = database.Login("master_password_1");	// Will return null
user = database.Login("master_password_2");			// Will also return null
user = database.Login("master_password_3");			// Will return a IUser this time
```

**Important — no rollback on a wrong passkey.** Each `Login` call appends the
passkey to the in-memory onion stack. A mistyped value is never undone: further
`Login` calls keep stacking on top of it, so even the correct passkeys will keep
failing until you `Close()` the database and `Open` it again. That is intentional
(an online anti-brute-force friction layer on top of PBKDF2); see
[SECURITY.md](SECURITY.md#progressive-login-without-rollback-online-brute-force-friction).
In the GUI, cancelling the login (e.g. Escape) ends the session so the user can
restart cleanly.

Once the IUser retrieved, it allow a full access to all services and accounts, all log history and all user settings (`user.Settings`).

`IDatabase` also implements `IDisposable`: `Dispose()` closes the session the same
way as `Close()`. Prefer a `using` when you own the lifetime of the database.

### Saving the changes

Use the `IDatabase.Save` method to save the user's updates.
Note that any update on the user, its settings, services and/or accounts which is
not saved is kept in the `autosave` entry inside the `.pku` ZIP (not a separate file).

```csharp
user.Settings.LogoutTimeout = 5;	// Setting the logout timeout to 5 min writes the autosave entry
database.Save();					// Persists into the database entry and clears autosave
```

### Logout/Close a database

To logout and close the database, use the `IDatabase.Close` method.
All unsaved updates remain in the `autosave` ZIP entry until the next successful merge/save.

```csharp
database.Close();
```

### Import and Export

`ImportFromFile` / `ExportToFile` (and their `Async` twins) are routed by file
extension. Only `.json` and `.csv` are supported; any other extension fails.

*   **JSON** carries `Settings` and `Services` (with accounts). Each identifier
    is `{ "Type", "Value" }` (`IdentifierType` + string).
*   **CSV** uses JSON-encoded cells. The `Identifiers` cell is pipe-joined
    **values only** (type is not stored); on import, types are re-detected
    (phone → `PhoneNumber`, email → `Email`, else `Username`). Import accepts
    **comma- or tab-delimited** rows; export writes **tab-separated** rows.
    Headers are `ServiceName`, `ServiceUrl`, `ServiceNotes`, `AccountLabel`,
    `Identifiers`, `Password`, `AccountNotes`, `AccountOptions`,
    `PasswordUpdateReminderDelay`. Settings are not included in CSV.

Import requires a logged-in user. Export and import files are **plaintext** — see
[SECURITY.md](SECURITY.md#known-limitations). A successful import already
persists (and both import and export save pending dirty state first). Export
fails if the destination file already exists.

### Keeping a UI responsive

Every expensive operation has an `Async` twin: `Database.CreateAsync`,
`Database.OpenAsync`, `IDatabase.LoginAsync`, `SaveAsync`, `ImportFromFileAsync`
and `ExportToFileAsync`.

They matter because the work behind them is deliberately slow: stretching a
single passkey costs about a second by design (see
[SECURITY.md](SECURITY.md#master-passkeys-multi-factor-onion)), and creating a
database also mints an RSA-4096 key pair. Running that on a UI thread freezes
the window for the whole duration.

```csharp
IDatabase database = await Database.OpenAsync(cryptographyCenter,
   serializationCenter,
   passwordFactory,
   clipboardManager,
   secretMemoryProtector,
   "./database.pku",
   "username");

IUser? user = await database.LoginAsync("master_password_1");
user = await database.LoginAsync("master_password_2");
user = await database.LoginAsync("master_password_3");	// Returns the IUser

await database.SaveAsync();
```

Two things to keep in mind:

*   These operations share the progressive passkey stack and the database file,
    so they are not meant to overlap: await one before starting the next.
*   Their events (`AutoSaveDetected`, `DatabaseSaved`, Core alert kind events / `CoreAlertsScanCompleted`,
    `DatabaseClosed`) are raised from the worker thread, so a handler touching UI
    state has to marshal back to its own thread.

`IPasswordFactory` follows the same pattern with `GeneratePasswordAsync` and
`PasswordLeakedAsync`. Those two are genuinely asynchronous rather than merely
offloaded: they await the leak-check providers (Have I Been Pwned first, then
XposedOrNot if HIBP is unreachable, then an optional local Bloom filter if both
remote providers fail) instead of blocking a thread on the network.

**Offline leak database (optional)**
------------------------------------

When HIBP and XposedOrNot are both unreachable, Passkey can fall back to a
local Bloom filter built from the HIBP NTLM corpus:

*   File: `<exe>/pwned-ntlm.pkbf` (size depends on quality: ~2.4 GiB Balanced,
    ~3.5 GiB Strict, ~4.7 GiB Paranoid) — path fixed in the WPF host (not stored
    in `config.json`)
*   Sidecar: `<filter>.pkbf.ranges` (~32 MiB), one fixed-width record per hash-range prefix holding the `ETag` already folded into the filter
*   Config in `config.json`: `LocalLeakDatabaseEnabled`,
    `LocalLeakDatabaseQuality` (`Balanced` / `Strict` / `Paranoid`; default
    **Balanced**), and `LocalLeakDatabaseAutoUpdateFrequency` (days; default
    **7**; **0** = off) — backed by `LeakFilterConfig` — **application-level**,
    shared by all vault users (not stored in the `.pku`)
*   Order: HIBP → XposedOrNot → Bloom (if enabled and present) → fail-open
*   Quality (target false-positive rate) applies only on a full build or rebuild;
    incremental updates keep the on-disk bit-array sizing
*   Disable never deletes the file; only **Delete offline database** in **App Settings** (or deleting the `.pkbf` manually) removes it — the sidecar goes with it
*   Build / update / enable / delete from **App Settings** (`Ctrl+,`, section **Offline leak database**), or from your own host through `HibpBloomBuilder.RunAsync`
*   **Auto-update** (`LocalLeakDatabaseAutoUpdateFrequency`): at WPF startup, if
    offline use is enabled, a `.pkbf` and its `.ranges` sidecar already exist,
    and the filter header `BuiltUtc` is older than the configured number of days,
    an incremental refresh runs in the background. A missing file never triggers
    an automatic first build. A missing sidecar skips the refresh and keeps the
    existing `.pkbf` (use **Rebuild** in App Settings to restore incremental
    updates).

A full build downloads every HIBP range (~1 048 576 prefixes) and can take several
hours. That is tens of GiB over the wire — brotli/gzip roughly halves the ~78 GB
of raw hex — so the build checkpoints every 4 096 prefixes into the `.building`
pair: an interrupted run resumes from the last checkpoint instead of restarting
the corpus.

An update never rebuilds and never changes Bloom sizing (a different quality
preset needs an explicit `Rebuild`). `HibpBloomBuildMode.Update` replays every
range with `If-None-Match` against the sidecar's ETags — unchanged ranges answer
`304` with no body — and folds only the changed ones into the existing bit
array. Requests run concurrently (default parallelism **64**, pooled HTTP/2
connections) so a refresh is dominated by round trips rather than bytes: every
prefix is revalidated, but only a few tens of MiB come down. This works because
Bloom filters are closed under union and the HIBP corpus only ever grows, so
inserting into the filter already on disk is equivalent to rebuilding it from
the whole corpus.

Two invariants keep that shortcut safe:

*   A checkpoint snapshots the pending entries, *then* flushes the filter, *then*
    persists the entries. An interruption can only leave the sidecar behind the
    filter, never ahead of it.
*   The sidecar records the `(InsertedCount, BuiltUtc)` stamp of the filter header
    it was written against. A filter that was rebuilt, restored or replaced no
    longer matches, and the sidecar is then rejected: skipping ranges whose bits
    are absent would mean reporting a leaked password as clean.

A rejected or missing sidecar on **Update** / auto-update skips the download and
keeps the existing `.pkbf` (use **Rebuild** when you need a fresh corpus and a
new sidecar). A rejected sidecar on a deliberate rebuild costs a full
re-download.

**WPF client (Windows)**
------------------------

The desktop app lives in `GUI/WPF`. It is MVVM with a small service locator
(`AppServices`) instead of a DI container, so ViewModels stay unit-testable.

*   **Localization**: English + French; app default in `config.json` is `System` (follow OS UI language when a satellite ships), per-user override in User settings. Activity and enum labels are localized at display time (`ActivityViewModel`, `EnumDisplayHelper`). Open windows refresh live via `{loc:Loc}` plus the Window `ILanguageAware` contract (see [Wiki/WPF-Client.md](Wiki/WPF-Client.md#localization)).
*   **Import / export UI**: User settings menu — Import (`.json` / `.csv`, comma- or tab-delimited) and Export → JSON / CSV (tab-separated). Success and failure dialogs are generic; the localized reason appears in the Activities grid.
*   **Vault files**: new users go under **App Settings → Default database directory**
    (`DefaultDatabaseDirectory`, default `<exe>/raw`) as `{GetHash(username)}.pku`,
    or another path chosen in the save dialog. Opening by username alone resolves
    the same `{DefaultDatabaseDirectory}/{hash}.pku` — prefer `Ctrl+O` or a
    command-line path when the vault is elsewhere.
*   **Login**: username, then each passkey in order. Escape cancels and closes
    the half-open session (required: there is no passkey rollback). App Settings
    `LoginIdleTimeoutSeconds` (default 5; `0` = off, not recommended) clears
    credentials on login-window inactivity; the title bar shows the countdown while armed.
*   **Credential confirmation**: creating a vault, or saving a username /
    ordered-passkey change in User settings, opens `CredentialsConfirmationView`
    (re-type **new** on create; **old then new** on update). Vault **Delete** and
    opening **User Settings** require **old** credentials. **Export** shows a
    plaintext warning only (no second credentials prompt). Progressive entry without
    rollback (intentional poison until Escape, like login — not a dialog bug).
    Closing the dialog cancels the action. Host-side intentionality /
    anti-mistype only — see [SECURITY.md](SECURITY.md) and
    [Wiki/WPF-Client.md](Wiki/WPF-Client.md).
*   **Shortcuts**: `Ctrl+O` open, `Ctrl+N` new user, `Ctrl+,` App Settings,
    `Ctrl+P` password generator. While the services window is open,
    **Ctrl+Shift+L** pastes the selected identifier and **Ctrl+Shift+P** pastes
    the selected password into the focused field (copy + synthetic Ctrl+V;
    clipboard still auto-clears).
*   **Offline leak database**: App Settings can build / update / enable / delete
    the local `.pkbf` Bloom filter next to the executable, and schedule
    auto-refresh of an existing file at startup
    (`LocalLeakDatabaseAutoUpdateFrequency` days; see Offline leak database above).
    Closing while a build/update is running prompts Yes / No / Cancel (finish
    after locking the vault, cancel, or stay open).
*   **QR codes**: identifiers and passwords can be shown as a QR matrix generated
    in-process (`Core/Utils/QrCode.cs`, no network). The window closes after
    `ISettings.ShowPasswordDelay` milliseconds when that setting is non-zero.
*   **Theme**: App Settings default (`System` / `Light` / `Dark`, stored in
    `config.json`); each vault user can override it. `System` follows Windows
    light/dark. Light and dark WPF dictionaries plus matching immersive title bars.
*   **Logs**: rolling daily files under `%LocalAppData%\Passkey\logs`.

**Testing**
-----------

### Automated

*   **Core / Utils**: `UnitTests/Multiplateform` (`net10.0`) covers crypto, vault
    lifecycle, import/export, persistence, and related models. Activity checks use
    structured `ExpectedActivity` fields (not localized UI strings). Runs on both
    Windows and Linux solutions.
*   **GUI ViewModels**: `UnitTests/Windows` references the WPF app and exercises
    ViewModels (`UnitTests/Windows/Gui/`) through a replaceable `AppServices` seam
    and fakes. Localized activity rendering is covered there (`ActivityAssertHelper`).
    No UI automation (FlaUI / WinAppDriver): login `PasswordBox`, hotkeys, themed
    confirmation dialogs (`ThemedMessageBoxView` via `DialogService`), and
    `CredentialsConfirmationView` stay out of the automated suite. ViewModel
    coverage for the confirmation sequence is in `CredentialsConfirmationViewModelTests`.
*   **Coverage**: `coverage.runsettings` measures **Core only** (Utils is a
    separate assembly and is not in that gate). Windows and Linux CI fail the
    build if line coverage drops below **90%**. `run_code_coverage.bat` and
    Windows CI write reports under `_testResult/` (gitignored).

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Linux.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Windows.Gui"
```

### Manual smoke (GUI)

After changes that touch login, clipboard, or hotkeys, verify on Windows:

1.  Create a new vault (multi-passkey): complete the **new** credentials
    confirmation dialog, then reopen with the same ordered passkeys.
2.  Mistype a passkey, then close/reopen and log in correctly (progressive login, no rollback).
3.  Change username or a master passkey in User settings: confirm **old** then
    **new** credentials; closing the dialog must skip the save; session ends after a successful change.
4.  Change only non-credential settings: no credentials dialog.
5.  Export JSON or CSV: confirm the **plaintext warning**, then pick the file (no second credentials prompt).
6.  Delete vault: after the two Yes dialogs, confirm **old** credentials (cancel skips delete).
7.  Copy an account password; confirm the clipboard clears after the configured timeout.
8.  Idle until auto-logout; confirm the session closes and the vault file is released.
9.  Use the Ctrl+Shift paste hotkeys on a focused field (identifier / password).
10. Show a password as a QR code and confirm the window closes after the configured delay.
11. Close while an offline leak-database build/update is running: Yes / No / Cancel
    (finish after vault lock, cancel, or stay open).

**CI**
------

GitHub Actions on `master` and pull requests:

| Workflow | What it does |
| -------- | ------------ |
| `.github/workflows/csharp-dotnet-windows.yml` | Restore, **versions.json sync check**, Debug + Release build, tests with Cobertura, **90% Core line-coverage gate** |
| `.github/workflows/csharp-dotnet-linux.yml` | Restore, **versions.json sync check**, Debug + Release build of the Linux solution (Interfaces + Utils + Core + Multiplateform tests); `dotnet test` runs Multiplateform with the **90% Core line-coverage gate** |
| `.github/workflows/codeql.yml` | CodeQL on every push (any branch) and weekly; Release build of production projects (tests excluded); SARIF filtered for `bin`/`obj`/`*.g.cs` |
| `.github/workflows/publish-wiki.yml` | On `Wiki/**` changes to `master`: publish the `Wiki/` folder to the GitHub Wiki |

There is currently **no** automated `release.yml`. After tagging, publish GitHub Releases manually. See [CONTRIBUTING.md](CONTRIBUTING.md#cutting-a-release).

Dependabot is configured for the **`github-actions`** ecosystem only (pinned
workflow SHAs). Test NuGet packages (MSTest, FluentAssertions) are not
auto-bumped.

**Getting Started**
-------------------

End users: download the Windows x64 zip
(`Upsilon.Apps.Passkey.GUI.WPF-*-win-x64.zip`) from
[Releases](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/releases)
(.NET 10 is bundled; Windows 10 1809 / build 18362 or later).

To build from source:

1.  Clone the repository: `git clone https://github.com/YassinLokhat/Upsilon.Apps.Passkey.git`
2.  Windows (GUI + tests): `dotnet build Upsilon.Apps.Passkey.Windows.slnx` then `dotnet run --project GUI/WPF`
3.  Linux (Interfaces + Utils + Core): `dotnet build Upsilon.Apps.Passkey.Linux.slnx`

Requires the .NET 10 SDK. The WPF app targets `net10.0-windows10.0.18362.0`.

**Contributing**
------------

See [CONTRIBUTING.md](CONTRIBUTING.md) for layout, the zero-dependency policy,
style rules, coverage, and what a PR should include. Security reports go through
[SECURITY.md](SECURITY.md), not public issues.

**License**
-------

This project is licensed under the GNU General Public License v2.0. See the [LICENSE](LICENSE) file for details.
