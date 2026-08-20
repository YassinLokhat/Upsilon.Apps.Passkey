# Warnings and Activity

The vault computes **warnings** locally (except opt-in leak checks, which call k-anonymity APIs). The **activity log** is an audit trail stored in the `.pku` `activity` entry.

## Warning types (`WarningType` flags)

| Flag | When it fires |
| ---- | ------------- |
| `ActivityReviewWarning` | Activities marked `NeedsReview` (failed login, possible tamper, …) |
| `PasswordUpdateReminderWarning` | Current password older than `IAccount.PasswordUpdateReminderDelay` months (`0` on the account means never) |
| `DuplicatedPasswordsWarning` | The same secret appears on more than one account, and **at least one** account in that group has `AccountOption.WarnIfDuplicatedPassword` (the warning lists every account in the group) |
| `PasswordLeakedWarning` | Opt-in leak check (`AccountOption.WarnIfPasswordLeaked`) found the password in a corpus |

`ISettings.WarningsToNotify` filters what is surfaced to the user. Subscribe to `IDatabase.WarningsUpdated` (`WarningsUpdatedEventArgs.Warnings`). Each `IWarning` may point at related `IActivity` rows and/or `IAccount`s.

Duplicate and expiry warnings are computed locally. Leak warnings use `IPasswordFactory.PasswordLeakedAsync` (HIBP then XposedOrNot). The UI does **not** surface a separate "could not verify" state: a transient failure is expected to succeed later; a lasting failure means the machine is offline or both providers are down — not actionable for a local-only tool. See [[Security]].

## Activity log (`IActivity`)

| Property | Meaning |
| -------- | ------- |
| `DateTime` | When the event was recorded |
| `ItemId` | Related item, or empty for vault-level events |
| `EventType` | `ActivityEventType` |
| `Message` | Human-readable line (never contains `ProtectedSecret` plaintext; `ToString()` on secrets is `***`) |
| `NeedsReview` | Drives `ActivityReviewWarning` |

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
      // Surface in UI; NeedsReview should already feed ActivityReviewWarning
   }
}

foreach (IWarning warning in database.Warnings ?? [])
{
   if (warning.WarningType.HasFlag(WarningType.PasswordLeakedWarning))
   {
      foreach (IAccount account in warning.Accounts ?? [])
      {
         // Prompt the user to rotate account.Password
      }
   }
}
```
