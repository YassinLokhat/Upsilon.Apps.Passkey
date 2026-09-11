# Usage Cookbook

Worked examples against Core. Replace `OsClipboardManager` with your `IClipboardManager`. Utils ships `SecretMemoryProtector` for in-memory wrapping. Prefer the `Async` twins from a UI.

## Create a vault and add an account

```csharp
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Utils;

var crypto = new CryptographyCenter();
var json = new JsonSerializationCenter();
var passwords = new PasswordFactory();
var clipboard = new OsClipboardManager();
var secretProtector = new SecretMemoryProtector();

IDatabase database = await Database.CreateAsync(
   crypto, json, passwords, clipboard, secretProtector,
   "./alice.pku",
   "alice",
   ["correct-horse", "battery-staple"]);

IUser user = database.User!;

string alphabet = passwords.Alphabetic + passwords.Numeric + passwords.SpecialChars;
string secret = await passwords.GeneratePasswordAsync(24, alphabet, checkIfLeaked: true);
if (string.IsNullOrEmpty(secret))
{
   // Leak-checked generation exhausted its retry budget.
   secret = passwords.GeneratePassword(24, alphabet, checkIfLeaked: false);
}

IService github = user.AddService("GitHub");
github.Url = new Uri("https://github.com");
github.Notes = "Work organisation";

IAccount work = github.AddAccount(
   "work",
   [
      new Identifier(IdentifierType.Email, "alice@company.com"),
      new Identifier(IdentifierType.Username, "alice-backup"),
   ],
   secret);
work.PasswordUpdateReminderDelay = 6;
work.Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;

await database.SaveAsync();
database.Close();
```

## Open and log in (correct order)

```csharp
IDatabase database = await Database.OpenAsync(
   crypto, json, passwords, clipboard, secretProtector,
   "./alice.pku",
   "alice");

IUser? user = await database.LoginAsync("correct-horse"); // null — onion incomplete
user = await database.LoginAsync("battery-staple");       // IUser when this was the last key

if (user is null)
{
   database.Close();
   throw new InvalidOperationException("Login failed; close and reopen after a typo.");
}
```

## Mistyped passkey (no rollback)

```csharp
IDatabase database = Database.Open(crypto, json, passwords, clipboard, secretProtector, "./alice.pku", "alice");

_ = database.Login("correct-horse");
_ = database.Login("batery-staple");  // typo — session is poisoned
_ = database.Login("battery-staple"); // still fails; the stack never rolls back

database.Close();
database = Database.Open(crypto, json, passwords, clipboard, secretProtector, "./alice.pku", "alice");
_ = database.Login("correct-horse");
IUser user = database.Login("battery-staple")!;
```

In the WPF client, **Escape** closes the half-open session so the user can restart cleanly.

## Autosave, then merge on next login

```csharp
database.AutoSaveDetected += (_, e) =>
{
   e.MergeBehavior = AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile;
};

user.Services.First().Accounts.First().Password = "new-secret"; // writes autosave (debounced)
await database.SaveAsync(); // promotes to database, clears autosave
```

If the process dies before `Save`, the next `Login` that completes raises `AutoSaveDetected`. Choose a [[Vault Format]] merge behaviour in the handler (including from a UI dialog — marshal first).

## Keep a WPF (or other UI) responsive

```csharp
database.AutoSaveDetected += (_, e) =>
{
   dispatcher.Invoke(() =>
   {
      e.MergeBehavior = AskUserHowToMerge(); // dialog on the UI thread
   });
};

database.DatabaseClosed += (_, e) =>
{
   dispatcher.Invoke(() =>
   {
      if (e.LoginTimeoutReached)
      {
         ShowIdleLogoutMessage();
      }
   });
};

IUser? user = await database.LoginAsync(passkey, cancellationToken);
```

Do not start a second `LoginAsync` / `SaveAsync` on the same instance until the first has finished.

## Subscribe to Core alerts

```csharp
database.PasswordLeakedAlertsChanged += (_, e) =>
{
   foreach (IAccountsAlert alert in e.Alerts.OfType<IAccountsAlert>())
   {
      foreach (IAccount account in alert.Accounts)
      {
         // Surface in UI; severity is on alert.Severity
      }
   }
};

database.CoreAlertsScanCompleted += (_, _) =>
{
   // All kinds for this scan are published; host can also call RefreshAlerts()
   // after app-level posture changes (idle login, offline Bloom filter).
};

// Unfiltered Core snapshots:
if (database.CoreAlerts.TryGetValue(AlertKinds.InsufficientPasskeys, out var insufficient)
   && insufficient.Count > 0)
{
   // Fewer than AlertKinds.RecommendedPasskeyCount onion layers
}
```

The WPF client aggregates Core + host alerts in `AlertBroker` and filters with `AlertsToNotify` — see [[Alerts and Activity]].

## Copy a password with auto-clear

```csharp
IAccount account = user.Services.First().Accounts.First();
database.ClipboardManager.SetText(
   account.Password,
   autoClearAfter: user.Settings.CleaningClipboardTimeout);
```

`SetText` only clears later if the clipboard still holds that same text, so an unrelated copy the user made afterwards is left alone. The WPF paste hotkeys (`Ctrl+Shift+L` / `Ctrl+Shift+P`) go through this same path before synthesizing Ctrl+V.

## Check whether a candidate is leaked

```csharp
bool leaked = await database.PasswordFactory.PasswordLeakedAsync(candidate);
```

This is the only outbound network the application makes (unless both remotes fail and a local Bloom filter answers offline). It is k-anonymity (hash prefix only). Order: HIBP → XposedOrNot → optional `.pkbf` → fail-open. See [[Security]].

## Dispose / `using`

```csharp
using IDatabase database = Database.Open(crypto, json, passwords, clipboard, secretProtector, "./alice.pku", "alice");
// Login, work, Save...
// Dispose → Close; file handle released
```

## Change passkeys or username (host responsibility)

`IUser.Passkeys` and `IUser.Username` are writable. Changing them changes the onion key material on the next `Save`. After a username change, the WPF client also stores the file under a different `GetHash(username)` name for new vaults — if you host Core yourself, you must decide whether to rename the `.pku` path (`IDatabase.DatabaseFile`).

Treat passkey rotation like any other secret change: the user must remember the new ordered set, and there is no server-side recovery.
