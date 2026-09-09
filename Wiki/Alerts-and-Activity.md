# Alerts and Activity

Alerts are published by **independent sources** (Core vault scans, host/GUI posture). Each alert implements a minimal `IAlert` (`Source`, `Kind`, `Severity`). Typed payloads live on kind-specific interfaces (`IActivityReviewAlert`, `IAccountsAlert`, `IVaultSecuritySettingsAlert`, passkey interfaces, `IHostSecuritySettingsAlert`). The **WPF client** aggregates sources in `AlertBroker` and filters with `ISettings.AlertsToNotify` (an `AlertKindList` of kind ids).

## Alert kinds (`AlertKinds`)

| Kind | Source | Severity | When it fires |
| ---- | ------ | -------- | ------------- |
| `ActivityReview` | Core | Max over rows: Critical (login-failed / tamper / session-timeout / **export**), Warning (`ItemUpdated` / `ItemDeleted`, …), Info (import / `ItemAdded` / autosave-merge) | Activities with `NeedsReview` |
| `PasswordUpdateReminder` | Core | Critical | Current password older than `IAccount.PasswordUpdateReminderDelay` months (`0` = never) |
| `DuplicatedPasswords` | Core | Warning | Same secret on ≥2 accounts, and **at least one** has `AccountOption.WarnIfDuplicatedPassword` |
| `PasswordLeaked` | Core (+ `IPasswordFactory`) | Critical | Opt-in leak check found the password in a corpus |
| `VaultSecuritySettings` | Core | Warning | Protective **vault** settings off — see `SecuritySettingsIssue` below |
| `InsufficientPasskeys` | Core | Critical if 1 layer; else Warning | Fewer than `AlertKinds.RecommendedPasskeyCount` (2) onion passkeys |
| `WeakPasskey` | Core | Critical | A passkey fails `SecretQuality` (length / classes / trivial / matches username) |
| `PasskeyLeaked` | Core (+ `IPasswordFactory`) | Critical | A passkey is found in a leak corpus |
| `WeakAccountPassword` | Core | Warning | An account password fails `SecretQuality` |
| `PasskeyReusedAsAccountPassword` | Core | Critical | A passkey equals an account password |
| `HostSecuritySettings` | Host (GUI) | Warning | App-level posture — see `HostSecurityIssue` |

### Vault security issues (`SecuritySettingsIssue`)

`AutoLogoutDisabled`, `ClipboardCleaningDisabled`, `QrAutoCloseDisabled`, `NoAccountLeakCheck` / `NoAccountDuplicateCheck` / `NoAccountUpdateReminder` (each when **no** account has that opt-in; requires ≥1 account).

### Host security issues (`HostSecurityIssue`)

`IdleLoginDisabled` (login idle timeout `0`), `OfflineLeakFilterUnavailable` (no loaded `.pkbf`).

Notify preferences no longer feed vault security issues (the old `*NotificationsDisabled` feedback loop is gone). When `AlertsToNotify` is empty, the WPF client shows a **MessageBox**.

### Secret quality (`SecretQuality`)

Local heuristics (no network) for `WeakPasskey` / `WeakAccountPassword`:

* minimum length **12**
* at least **3** character classes among lower / upper / digit / special
* not equal to the username (case-insensitive)
* not a trivial run (all identical chars, or strict ascending/descending code points)

Payloads never include the secret plaintext — only indexes / issue flags / related accounts.

## How clients consume alerts

Core raises **per-kind** events (`ActivityReviewAlertsChanged`, …) with unfiltered snapshots, plus `CoreAlertsScanCompleted`. Prefer `IDatabase.CoreAlerts` for the latest Core map.

The WPF `AlertBroker` also publishes host alerts and applies `AlertsToNotify`. Menu colors come from `IAlert.Severity` (`AlertBroker.BrushFor`), not hard-coded kinds.

Legacy vaults may still store `AlertsToNotify` / `WarningsToNotify` as old `WarningType` flags; `AlertKindList` JSON migration maps them onto kind ids.

Duplicate and expiry alerts are local. Leak / passkey-leak checks use `IPasswordFactory.PasswordLeakedAsync` (HIBP → XposedOrNot → optional local `.pkbf`). Fail-open when unreachable. See [[Security]].

## Activity log (`IActivity`)

| Property | Meaning |
| -------- | ------- |
| `DateTime` | When the event was recorded |
| `ItemId` | Related item, or empty for vault-level events |
| `Username` / `ServiceName` / `AccountName` | Scope of the event (user, service, or account) |
| `FieldName` / `FieldValue` | Changed field and new value, or error context (e.g. `ImportExportError` + enum member name). Never contains `ProtectedSecret` plaintext; `ToString()` on secrets is `***`. |
| `ParentName` | Parent item name for account-level updates |
| `EventType` | `ActivityEventType` |
| `NeedsReview` | Drives `ActivityReview`. Clearing it (the Activities grid checkbox) is written back into the log on the next sealed persist (Save or logout). |

There is no persisted `Message` string. Core stores structured fields in a pipe-delimited wire format (`Activity.ToString()`). The **WPF** Activities grid builds localized text at display time from `EventType` + these fields via `ActivityViewModel` (`Activity_*` / `EnumValue_*` keys — see [[WPF Client]] Localization).

Retention is `ISettings.NumberOfMonthActivitiesToKeep`.

### Event kinds

Numeric values are a **persistence contract** and must stay stable. Mapping from `AutoSaveMergeBehavior` is explicit in code — do not rely on matching ordinals.

| Group | Values |
| ----- | ------ |
| Autosave merge | `MergeAndSaveThenRemoveAutoSaveFile`, `MergeWithoutSavingAndKeepAutoSaveFile`, `DontMergeAndRemoveAutoSaveFile`, `DontMergeAndKeepAutoSaveFile` |
| Session | `DatabaseCreated`, `DatabaseOpened`, `DatabaseSaved`, `DatabaseClosed`, `LoginSessionTimeoutReached`, `LoginFailed`, `UserLoggedIn`, `UserLoggedOut` |
| Import / export | `ImportingDataStarted`, `ImportingDataSucceded`, `ImportingDataFailed`, `ExportingDataStarted`, `ExportingDataSucceded`, `ExportingDataFailed` |
| Items | `ItemUpdated`, `ItemAdded`, `ItemDeleted` |
| Integrity | `ActivityLogTampered` |

## How the log is protected

Because entries must be writable **without being logged in**, writing relies on the **public** RSA key alone and therefore cannot be protected by a secret. Integrity is provided by **sealing**, which makes tampering *detectable* on the next login:

* On every save **while a user is logged in**, the whole current log is sealed: an **RSA-PSS-SHA256** signature (user's private key) over a canonical payload of the sealed entry count, the activity log's public key, and the sealed entry ciphertexts. Verification only needs the public key.
* The number of sealed entries is anchored inside the **encrypted, AEAD-protected database** (`ActivitySealWatermark`). That lets the next login detect a **rollback/truncation** of the sealed entries, or a **stripped** signature.
* On login the stored public key must match the key pair in the database (defeats **key substitution**), and the signature must be valid over the sealed entries (defeats **modification, forgery, and reordering** of the sealed portion).
* If any check fails, login is **not** blocked. A reviewable `ActivityLogTampered` activity is recorded so the user is alerted.

Each record itself is hybrid-encrypted: random AES key wrapping the payload, key wrapped with **RSA-OAEP-SHA256**. That is why a failed login can still append a ciphertext the legitimate user can read later.

### Unsealed tail (limitation)

Entries added since the last logged-in save — including events written while no one is logged in, such as failed logins — are **not** protected against deletion or alteration by an attacker with write access to the file. Detecting that fully would require a trusted external log; it is out of scope for a purely local tool. Everything sealed at the last login remains tamper-evident. See [[Threat Model]].

## Concrete usage: review failed logins

```csharp
foreach (IActivity activity in database.Activities ?? [])
{
   if (activity.EventType == ActivityEventType.LoginFailed
      || activity.EventType == ActivityEventType.ActivityLogTampered)
   {
      // Surface in UI; NeedsReview should already feed ActivityReview
   }
}

if (database.CoreAlerts.TryGetValue(AlertKinds.PasswordLeaked, out var leaked)
    && leaked.OfType<IAccountsAlert>().FirstOrDefault() is { } alert)
{
   foreach (IAccount account in alert.Accounts)
   {
      // …
   }
}
```
