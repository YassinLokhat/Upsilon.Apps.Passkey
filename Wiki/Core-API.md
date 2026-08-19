# Core API

Public contracts live in `Upsilon.Apps.Passkey.Interfaces`. The vault implementation is `Upsilon.Apps.Passkey.Core.Models.Database` plus the `Core.Utils` defaults.

## Factories

```csharp
IDatabase Database.Create(
   ICryptographyCenter cryptographicCenter,
   ISerializationCenter serializationCenter,
   IPasswordFactory passwordFactory,
   IClipboardManager clipboardManager,
   string databaseFile,
   string username,
   string[] passkeys);

IDatabase Database.Open(
   ICryptographyCenter cryptographicCenter,
   ISerializationCenter serializationCenter,
   IPasswordFactory passwordFactory,
   IClipboardManager clipboardManager,
   string databaseFile,
   string username);
```

* `Create` — the file must **not** already exist (`IOException` if it does). Directories on the path are created. Returns an **already logged-in** database (`User` is set). Do not call `Login` afterwards.
* `Open` — the file must exist. `User` stays `null` until progressive login succeeds with every passkey, **in order**.
* Async twins: `CreateAsync`, `OpenAsync`. Prefer them from a UI thread (RSA-4096 keygen plus one PBKDF2 stretch per passkey).

Default implementations except clipboard: `CryptographyCenter`, `JsonSerializationCenter`, `PasswordFactory`.

## `IDatabase`

| Member | Notes |
| ------ | ----- |
| `DatabaseFile` | Path to the `.pku` |
| `User` | `null` until login completes (except after `Create`) |
| `SessionLeftTime` | Seconds remaining before auto-logout; `null` when logged out |
| `Activities` / `Warnings` | Current audit trail and computed warnings |
| `Login` / `LoginAsync` | Append one stretched passkey. Returns `IUser` only on the last correct key. **No rollback.** |
| `Save` / `SaveAsync` | Persist the logged-in user. Throws `NullValueException` if not logged in. Clears autosave. |
| `Delete` | Delete the vault file. Requires login. |
| `Close` / `Dispose` | End the session. Unsaved work remains in the `autosave` ZIP entry. |
| `HasChanged(itemId)` / `HasChanged(itemId, fieldName)` | Dirty tracking for UI |
| `ImportFromFile` / `ExportToFile` (+ Async) | `.json` or `.csv` only — see [[Import Export]] |

Events: `AutoSaveDetected`, `DatabaseSaved`, `WarningsUpdated`, `DatabaseClosed` (`LogoutEventArgs.LoginTimeoutReached` tells you whether idle timeout closed the session).

### Async rules

These operations share the progressive passkey stack and the database file. They are not meant to overlap: **await one before starting the next**.

Events are raised from the **worker thread**. A handler that touches UI state must marshal back to its own thread. See [[Usage Cookbook]].

`IPasswordFactory.GeneratePasswordAsync` and `PasswordLeakedAsync` are genuinely asynchronous (they await leak-check providers) rather than merely offloaded.

## Progressive login

Each `Login` call appends the passkey to the in-memory onion stack and attempts decryption. A mistyped value is **never undone**. Further `Login` calls keep stacking on top of it, so even the correct remaining passkeys fail until you `Close()` and `Open` again.

That is intentional online anti-brute-force friction on top of PBKDF2 — not a UX bug. Details: [[Security]].

`Login` returns `null` for an incomplete onion or a wrong passkey (both caught internally). It throws `CorruptedSourceException` when the database entry is corrupted or not a Passkey vault payload.

## `IUser`, `IService`, `IAccount`

```csharp
user.Settings.LogoutTimeout = 15;             // minutes
user.Settings.CleaningClipboardTimeout = 20;  // seconds
user.Passkeys = ["pk1", "pk2"];               // changing passkeys rewrites the onion on Save

IService mail = user.AddService("Proton");
IAccount account = mail.AddAccount("personal", ["me@pm.me"], generated);
account.Notes = "2FA on hardware key";
account.PasswordUpdateReminderDelay = 6; // months; 0 = never
account.Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;
```

`IAccount.Passwords` is dated history. Length is capped by `ISettings.NumberOfOldPasswordToKeep`. `IAccount.Identifiers` are logins, emails, or other labels for that account.

`AddAccount` has overloads that omit the label, the password, or both (password can be generated later).

## `ISettings`

| Property | Unit | Role |
| -------- | ---- | ---- |
| `LogoutTimeout` | minutes | Inactivity auto-logout |
| `CleaningClipboardTimeout` | seconds | Clipboard auto-clear |
| `ShowPasswordDelay` | milliseconds | QR window auto-close (`0` = until dismissed). Named historically for password reveal; the WPF client uses it for QR display. |
| `NumberOfOldPasswordToKeep` | count | Password history cap |
| `NumberOfMonthActivitiesToKeep` | months | Activity retention |
| `WarningsToNotify` | `WarningType` flags | Which warnings to surface — [[Warnings and Activity]] |

Any mutation that is not yet `Save()`d is kept in the `autosave` ZIP entry.

## Crypto, passwords, clipboard

### `ICryptographyCenter`

Fast hash, slow hash (PBKDF2), onion encrypt/decrypt, RSA-4096 PEM keygen, hybrid asymmetric encrypt, sign (`RSA-PSS`) / verify. `EnsureSufficientSlowHashParameters` is the KDF floor used on Open.

### `IPasswordFactory`

CSPRNG over `Alphabetic`, `Numeric`, and `SpecialChars`. When `checkIfLeaked` is true, generation retries at most **five** candidates against the leak corpora and then gives up (returns empty) rather than hammering the remote service.

Leak detection: Have I Been Pwned range API first (`api.pwnedpasswords.com` — first 5 characters of SHA-1), then XposedOrNot (`passwords.xposedornot.com` — first 10 characters of raw Keccak-512, not NIST SHA-3). The password itself never leaves the device. If HIBP answers definitively, XON is not contacted. If **both** are unreachable, the check **fails open** (reports "not leaked"). Failed checks are not cached; only successful answers are kept in process (never persisted).

### `IClipboardManager`

`SetText(text, autoClearAfter)` clears later **only if** the clipboard still holds that same text. The `int` overload is seconds (`0` or negative means no auto-clear). `RemoveAllOccurrenceAsync` scrubs clipboard history (WinRT APIs are async; do not block a UI or timer thread waiting on it).

## Exceptions hosts should expect

| Exception | When |
| --------- | ---- |
| `IOException` | `Create` when the `.pku` already exists |
| `WrongPasswordException` | Wrong onion (often swallowed by `Login`, which returns `null`) |
| `CorruptedSourceException` | Payload is not a Passkey vault, or AEAD failed as corruption |
| `InsufficientKdfParametersException` | Header below the KDF floor |
| `NullValueException` | `Save` / `Delete` / import without a logged-in user (and some deserialize paths) |
