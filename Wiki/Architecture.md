# Architecture

Upsilon.Apps.Passkey is four layers and two solution files. The only **OS-specific** dependency the host must supply is `IClipboardManager`. File I/O lives in Core (BCL). Opt-in HTTP leak checks live in Utils (`PasswordFactory`). Those are not injected ports.

## Repository layout

| Path | Role |
| ---- | ---- |
| `Interfaces/` | Public contracts (`IDatabase`, `IUser`, crypto, serialization, clipboard). |
| `Utils/` | Default implementations: `CryptographyCenter`, `JsonSerializationCenter`, `PasswordFactory`, `ProtectedSecret`. **Zero NuGet packages** (BCL only). |
| `Core/` | Vault implementation: onion encryption, `.pku` I/O, warnings, import/export. **Zero NuGet packages** (BCL only). Vault-internal helpers stay under `Core/Utils/` (`QrCode`, file lock, activity, import/export). |
| `GUI/WPF/` | Windows desktop client (MVVM + a small `AppServices` locator). |
| `UnitTests/` | Core/Utils tests plus ViewModel tests through the `AppServices` seam. |

| Solution | Projects |
| -------- | -------- |
| `Upsilon.Apps.Passkey.Windows.slnx` | Interfaces, Utils, Core, WPF GUI, UnitTests |
| `Upsilon.Apps.Passkey.Linux.slnx` | Interfaces, Utils, and Core only (no WPF, no tests: the test project targets `net10.0-windows`) |

The WPF app supplies `IClipboardManager` and hosts dialogs, session, and navigation behind `AppServices` so ViewModels stay unit-testable without a window.

## Domain graph

```mermaid
flowchart LR
  IDatabase --> IUser
  IUser --> ISettings
  IUser --> IService
  IService --> IAccount
  IDatabase --> IActivity
  IDatabase --> IWarning
  IDatabase --> ICryptographyCenter
  IDatabase --> ISerializationCenter
  IDatabase --> IPasswordFactory
  IDatabase --> IClipboardManager
```

`IUser`, `IService`, and `IAccount` implement `IItem` (stable `ItemId`, `HasChanged()`, back-reference to `IDatabase`). `IDatabase` implements `IDisposable`: `Dispose()` closes the session the same way as `Close()`.

## Class diagram

```mermaid
classDiagram
    direction LR

    namespace Upsilon.Apps.Passkey.Interfaces.Utils {
        class ISerializationCenter {
            <<interface>>
            +Serialize(in toSerialize T) string
            +Deserialize(in toDeserialize string) T
        }

        class IClipboardManager {
            <<interface>>
            +SetText(in text string, in autoClearAfter TimeSpan?) void
            +SetText(in text string, in autoClearAfter int) void
            +RemoveAllOccurrenceAsync(in removeList IEnumerable~string~, in cancellationToken CancellationToken) Task~int~
        }

        class IPasswordFactory {
            <<interface>>
            +string Alphabetic
            +string Numeric
            +string SpecialChars
            +GeneratePassword(in length int, in alphabet string, in checkIfLeaked bool) string
            +GeneratePasswordAsync(in length int, in alphabet string, in checkIfLeaked bool, in cancellationToken CancellationToken) Task~string~
            +PasswordLeaked(in password string) bool
            +PasswordLeakedAsync(in password string, in cancellationToken CancellationToken) Task~bool~
        }

        class ICryptographyCenter {
            <<interface>>
            +int HashLength
            +KdfParameters DefaultSlowHashParameters
            +GetHash(in source string) string
            +GetSlowHash(in source string, in parameters KdfParameters) string
            +EnsureSufficientSlowHashParameters(in parameters KdfParameters) void
            +EncryptSymmetrically(in source string, in passwords IEnumerable~string~) string
            +DecryptSymmetrically(in source string, in passwords IEnumerable~string~) string
            +GenerateRandomKeys(out publicKey string, out privateKey string) void
            +EncryptAsymmetrically(in source string, in key string) string
            +DecryptAsymmetrically(in source string, in key string) string
            +GetPublicKey(in privateKey string) string
            +Sign(in source string, in privateKey string) string
            +Verify(in source string, in signature string, in publicKey string) bool
        }

        class KdfParameters {
            +KdfAlgorithm Algorithm
            +int Iterations
            +int OutputLength
            +string Salt
        }
    }

    namespace Upsilon.Apps.Passkey.Interfaces.Models {
        class IItem {
            <<interface>>
            +string ItemId
            +IDatabase Database
            +HasChanged(void) bool
        }

        class IAccount {
            <<interface>>
            +IService Service
            +string Label
            +string Notes
            +IEnumerable~string~ Identifiers
            +string Password
            +Dictionary~DateTime_string~ Passwords
            +int PasswordUpdateReminderDelay
            +AccountOption Options
        }

        class IService {
            <<interface>>
            +IUser User
            +string ServiceName
            +Uri? Url
            +string Notes
            +IEnumerable~IAccount~ Accounts
            +AddAccount(in label string, in identifiers IEnumerable~string~, in password string) IAccount
            +DeleteAccount(in account IAccount) void
        }

        class IUser {
            <<interface>>
            +string Username
            +IEnumerable~string~ Passkeys
            +ISettings Settings
            +IEnumerable~IService~ Services
            +AddService(in serviceName string) IService
            +DeleteService(in service IService) void
        }

        class ISettings {
            <<interface>>
            +int LogoutTimeout
            +int CleaningClipboardTimeout
            +int ShowPasswordDelay
            +int NumberOfOldPasswordToKeep
            +int NumberOfMonthActivitiesToKeep
            +WarningType WarningsToNotify
            +string Language
            +string Theme
        }

        class IDatabase {
            <<interface>>
            +string DatabaseFile
            +IUser? User
            +int? SessionLeftTime
            +IEnumerable~IActivity~ Activities
            +IEnumerable~IWarning~ Warnings
            +ISerializationCenter SerializationCenter
            +ICryptographyCenter CryptographyCenter
            +IPasswordFactory PasswordFactory
            +IClipboardManager ClipboardManager
            +EventHandler~WarningsUpdatedEventArgs~ WarningsUpdated
            +EventHandler~AutoSaveDetectedEventArgs~ AutoSaveDetected
            +EventHandler DatabaseSaved
            +EventHandler~LogoutEventArgs~ DatabaseClosed
            +Login(in passkey string) IUser?
            +LoginAsync(in passkey string, in cancellationToken CancellationToken) Task~IUser~
            +Save(void) void
            +SaveAsync(in cancellationToken CancellationToken) Task
            +Delete(void) void
            +Close(void) void
            +HasChanged(in itemId string) bool
            +HasChanged(in itemId string, in fieldName string) bool
            +ImportFromFile(in filePath string) bool
            +ImportFromFileAsync(in filePath string, in cancellationToken CancellationToken) Task~bool~
            +ExportToFile(in filePath string) bool
            +ExportToFileAsync(in filePath string, in cancellationToken CancellationToken) Task~bool~
        }

        class IActivity {
            <<interface>>
            +DateTime DateTime
            +string ItemId
            +string? Username
            +string? ServiceName
            +string? AccountName
            +string? FieldName
            +string? FieldValue
            +string? ParentName
            +ActivityEventType EventType
            +bool NeedsReview
        }

        class IWarning {
            <<interface>>
            +WarningType WarningType
            +IEnumerable~IActivity~? Activities
            +IEnumerable~IAccount~? Accounts
        }
    }

    IUser --|> IItem
    IService --|> IItem
    IAccount --|> IItem
    IDatabase ..|> IDisposable
    IItem --> IDatabase : Database
    IAccount --> IService : Service
    IService --> IUser : User
    IUser --> ISettings : Settings
    IService "0" --> "*" IAccount : Accounts
    IUser "0" --> "*" IService : Services
    IDatabase --> IUser : User
    IDatabase "0" --> "*" IWarning : Warnings
    IDatabase "0" --> "*" IActivity : Activities
    IDatabase --> ISerializationCenter : SerializationCenter
    IDatabase --> ICryptographyCenter : CryptographyCenter
    IDatabase --> IPasswordFactory : PasswordFactory
    IDatabase --> IClipboardManager : ClipboardManager
```

Event-arg types (`WarningsUpdatedEventArgs`, `AutoSaveDetectedEventArgs`, `LogoutEventArgs`) and enums live under `Interfaces.Events` / `Interfaces.Enums` — see the fuller diagram in the repository `README.md`.

## Design choices that show up in usage

* **No DI container in WPF.** `AppServices` is a small locator so tests can swap session, dialogs, clipboard, and navigation.
* **Internal host surfaces.** `Database` is a partial class. Narrow internal hosts (`IActivityHost`, `IAutoSaveHost`, `IUserHost`) keep `ActivityCenter`, `AutoSave`, and `User` from digging into `Database` members (CodeQL `cs/coupled-types`). Public API stays on `IDatabase` / `IUser`.
* **Sticky KDF header** in the `.pku`. Reopen always uses the parameters stored in the file. There is no automatic upgrade to `DefaultSlowHashParameters` on save today. That header is the hook for a future work-factor or algorithm migration. See [[Vault Format]].
* **Deferred ZIP writes** (~500 ms debounce) while logged in. Pre-login audit events (open, failed login) still write immediately so the trail survives a crash before the session starts.
* **Zero-dependency Core, Utils, and Interfaces.** An MSBuild target fails the build if a third-party `PackageReference` appears. Supply-chain surface is the .NET BCL plus CI (CodeQL on GitHub runners). See [[Contributing]].
