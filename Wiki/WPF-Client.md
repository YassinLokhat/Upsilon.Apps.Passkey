# WPF Client

The Windows desktop app lives in `GUI/WPF`. It is MVVM with a small service locator (`AppServices`) instead of a DI container, so ViewModels stay unit-testable. The project currently has no NuGet packages either; keep it that way unless a Windows-only capability cannot be done with the BCL.

Target framework: `net10.0-windows10.0.18362.0`. Dark WPF resources plus Windows immersive dark title bars.

## Localization

UI strings live in `GUI/WPF/Localization/`:

* `Strings.resx` — English (neutral / fallback)
* `Strings.fr.resx` — French
* `LocalizationService.Supported` — combo-box registry
* `{loc:Loc KeyName}` in XAML; `Strings.KeyName` / `Strings.Format(...)` in C#

Language is an **app** setting (`config.json` next to the exe, property `Language`), not a vault setting. Change it under **App Settings** (`Ctrl+,`).

### Adding a language

1. Copy `Strings.resx` → `Strings.xx.resx` and translate values (keep key names).
2. Append `new("xx", "Native name")` to `LocalizationService.Supported`.
3. Run `LocalizationTests.FrenchResources_ContainEveryNeutralKey` pattern (or extend it) so every neutral key exists in the new satellite.

Do not put UI strings in Core, Utils, or Interfaces. Keep vault-persisted generated names (e.g. `New Service #`) culture-stable.

## Vault files and logs

* New users are stored next to the executable as `raw/{GetHash(username)}.pku`.
* `Ctrl+O` opens an existing `.pku`. A path can also be passed as the **first command-line argument**.
* Rolling daily logs under `%LocalAppData%\Passkey\logs`.

## Login

1. Username
2. Each passkey **in order**

Escape cancels and closes the half-open session. That is required: there is no passkey rollback (see [[Security]]).

The GUI keeps the typed secret in `PasswordBox.SecurePassword` and bridges it through `SecureStringExtensions.UseAsString`: the unmanaged BSTR is zeroed in a `finally` block (`Marshal.ZeroFreeBSTR`) so it only lives for the duration of the `Login` call. The short-lived managed `string` passed to Core remains subject to the usual .NET GC limitations.

`Create` already logs the user in; the new-user flow must not call `Login` again on that session.

## Shortcuts

| Shortcut | Action |
| -------- | ------ |
| `Ctrl+O` | Open vault |
| `Ctrl+N` | New user |
| `Ctrl+P` | Password generator |
| `Ctrl+Shift+L` | While the services window is open: paste the selected **identifier** into the focused field |
| `Ctrl+Shift+P` | Paste the selected **password** into the focused field |

Paste hotkeys copy via `IClipboardManager` then synthesize Ctrl+V. The clipboard still auto-clears after `ISettings.CleaningClipboardTimeout`, and Windows clipboard history is scrubbed through `RemoveAllOccurrenceAsync`.

## QR codes

Identifiers and passwords can be shown as a QR matrix generated **in-process** (`Core/Utils/QrCode.cs`, no network). The window closes after `ISettings.ShowPasswordDelay` milliseconds when that setting is non-zero (`0` means until dismissed). Anyone who can see or photograph the screen can capture the secret.

## Session protection

* **Auto-logout** after `LogoutTimeout` minutes of inactivity. The database file handle is released.
* On process exit, `AppServices.Session.EndSession()` closes any open vault and clears owned clipboard content, in case `MainWindow.Closed` did not run first.
* Unhandled UI exceptions are logged and marked handled so a single dialog failure does not tear down the process. AppDomain / unobserved-task exceptions are logged and flushed.

## Autosave in the GUI

Unsaved edits are kept in the `.pku` ZIP `autosave` entry. On the next login, `AutoSaveDetected` fires. The dialog must run on the UI thread because the event is raised from a worker thread when using `LoginAsync`.

The WPF client uses a Yes / No / Cancel prompt and maps it as follows:

| Dialog | `AutoSaveMergeBehavior` | Effect |
| ------ | ----------------------- | ------ |
| **Yes** | `MergeAndSaveThenRemoveAutoSaveFile` | Apply autosave, persist, remove the ZIP entry |
| **No** | `DontMergeAndRemoveAutoSaveFile` | Discard autosave and remove the ZIP entry |
| **Cancel** | `MergeWithoutSavingAndKeepAutoSaveFile` | Apply autosave **in memory only**, keep the ZIP entry |

`DontMergeAndKeepAutoSaveFile` is available on the enum (hosts / tests) but unused by the WPF dialog. The dialog wording (“Cancel to ignore”) means “do not decide yet”: Cancel still merges into the open session; it does not leave the in-memory model untouched. Full enum meanings: [[Vault Format]].

## Manual smoke (GUI)

After changes that touch login, clipboard, or hotkeys, verify on Windows:

1. Create a new vault (multi-passkey) and reopen it with the same ordered passkeys.
2. Mistype a passkey, then close/reopen and log in correctly (progressive login, no rollback).
3. Copy an account password; confirm the clipboard clears after the configured timeout.
4. Idle until auto-logout; confirm the session closes and the vault file is released.
5. Use the Ctrl+Shift paste hotkeys on a focused field (identifier / password).
6. Show a password as a QR code and confirm the window closes after the configured delay.

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global hotkeys, and real MessageBoxes stay out of the automated suite — [[Testing and CI]].
