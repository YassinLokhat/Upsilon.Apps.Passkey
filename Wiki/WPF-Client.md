# WPF Client

The Windows desktop app lives in `GUI/WPF`. It is MVVM with a small service locator (`AppServices`) instead of a DI container, so ViewModels stay unit-testable. The project currently has no NuGet packages either; keep it that way unless a Windows-only capability cannot be done with the BCL.

Target framework: `net10.0-windows10.0.18362.0`. Light and dark WPF resource dictionaries, with Windows immersive title bars matching the active appearance.

## Localization

UI strings live in `GUI/WPF/Localization/`:

* `Strings.resx` — English (neutral / fallback)
* `Strings.fr.resx` — French
* `LocalizationService.Supported` — combo-box registry
* `{loc:Loc KeyName}` in XAML (live binding via `TranslationSource`); `Strings.KeyName` / `Strings.Format(...)` in C#
* `LocalizationService.Apply` refreshes open windows implementing `ILanguageAware` (titles, combos, computed labels) — **no restart required**

Language is an **app** setting (`config.json`, property `Language`) under **App Settings** (`Ctrl+,`). Each vault user can **override** it under **User settings** (`ISettings.Language`). Empty user language = follow the app. On login the client applies the effective language; on logout it reverts to the app language. Open windows update immediately via `ILanguageAware`.

## Theme

Color brushes live in `GUI/WPF/Themes/DarkTheme.xaml` and `LightTheme.xaml` (same keys). Control styles in `Controls.xaml` use `{DynamicResource}` so a dictionary swap repaints open windows — **no restart required**. `ThemeService.Apply` also updates immersive title bars and notifies `IThemeAware` surfaces (code-behind brushes on account/service fields).

Theme is an **app** setting (`config.json`, property `Theme`: `System`, `Light`, or `Dark`) under **App Settings**. Each vault user can **override** it under **User settings** (`ISettings.Theme`). Empty user theme = follow the app. `System` follows Windows `AppsUseLightTheme`. On login the client applies the effective theme; on logout it reverts to the app theme. If the effective preference is `System`, an OS light/dark change is applied live.

Do not put UI strings in Core, Utils, or Interfaces. The vault persists **stable** data (enum member names, field names, `New Service #`); the WPF client localizes at display time.

### Key prefixes

| Prefix | Role | Example |
| ------ | ---- | ------- |
| `Menu_` | Menu items | `Menu_Save` → `_Save` |
| `Label_` | Labels, checkboxes, column headers | `Label_Username` |
| `Title_` | Window / dialog titles | `Title_UserSettings` → `{0} - User settings` |
| `Msg_` | MessageBox / busy / status text | `Msg_OpeningDatabase` |
| `Filter_` | File dialog filters and “All” | `Filter_Pku` |
| `IdentifierType_` | Insert-identifier buttons | `IdentifierType_Email` |
| `FieldName_` | Field names inside activity sentences | `FieldName_ServiceName` → `service name` |
| `EnumValue_*_` | Short enum labels (filters, combo boxes) | see below |
| `EnumValue_ImportExportError_*` | Import/export failure reasons in activity messages | `EnumValue_ImportExportError_NoDataToImport` → `no data to import` |
| `Activity_` | Full activity **Message** sentences | see below |

When you add a key: update `Strings.resx`, every satellite (e.g. `Strings.fr.resx`), and the typed accessor in `Strings.cs` unless the key is only loaded via `Strings.Get("…")` (dynamic `FieldName_*` / `EnumValue_*` lookups).

### Activity events: two keys per `ActivityEventType`

Each `ActivityEventType` (except `None`) usually needs **two** resource entries. They are not duplicates — they serve different UI surfaces.

| Key family | Used by | Shape | Example (`DatabaseOpened`) |
| ---------- | ------- | ----- | -------------------------- |
| `EnumValue_ActivityEventType_{Member}` | `EnumHelper.ToReadableString` → filter combo / Event type column | Short noun phrase, no placeholders | EN: `Database opened` · FR: `Base de données ouverte` |
| `Activity_{Member}` | `ActivityViewModel` → Message column | Full sentence; `{0}`, `{1}`, … for username / path / etc. | EN: `User '{0}'s database opened` · FR: `Base de données de l'utilisateur '{0}' ouverte` |

```
ActivityEventType.DatabaseOpened
        │
        ├─► EnumValue_ActivityEventType_DatabaseOpened   (filters / Event type)
        └─► Activity_DatabaseOpened                      (Message column)
```

**Translator checklist when adding or changing an event type:**

1. Add / update `EnumValue_ActivityEventType_<Member>` in every language file.
2. Add / update `Activity_<Member>` (and any variants such as `Activity_UserLoggedOutWithoutSaving`) with the correct placeholder count.
3. Wire the Message path in `ActivityViewModel`, `StringsHelper`, and/or `EnumDisplayHelper` depending on event shape.
4. Keep Core persistence unchanged: store enum names and field ids, never translated text.

Other enum labels follow the same `EnumValue_{EnumType}_{Member}` pattern (`EnumValue_WarningType_*`, optional `EnumValue_AccountOption_*`, `EnumValue_ImportExportError_*`, `EnumValue_Theme_*`). `EnumDisplayHelper.FormatFieldValue` localizes values stored in activity `FieldValue` (Core persists `Enum.ToString()` names, not translated text). Import/export failure reasons use `EnumValue_ImportExportError_{Member}`; theme preference values use `EnumValue_Theme_*`. Warning filter strings may reuse existing `Label_Notify*` keys via `EnumDisplayHelper` when the wording already matches the settings UI.

`FieldName_*` keys localize the middle of ItemUpdated-style sentences (`Strings.Get($"FieldName_{activity.FieldName}")`). If Core starts persisting a new field name, add a matching `FieldName_` entry or the UI falls back to the raw key.

### Adding a language

1. Copy `Strings.resx` → `Strings.xx.resx` and translate values (keep key names). Pay special attention to **both** `EnumValue_ActivityEventType_*` and `Activity_*` for every event.
2. Append `new("xx", "Native name")` to `LocalizationService.Supported`.
3. Run `LocalizationTests` — they loop every non-English entry in `Supported` (`SatelliteResources_ContainEveryNeutralKey`, etc.), so a new satellite is covered automatically once registered.

## User settings — import and export

While logged in, **User settings** offers **Import** (`.json` or `.csv`) and **Export → JSON / CSV**. Unsaved edits are saved first after confirmation (`Msg_SaveBeforeContinue`). Success and failure dialogs are generic (`Msg_ImportSuccess` / `Msg_ImportFailed`, etc.); the localized reason appears in the Activities grid (`ImportingDataFailed` / `ExportingDataFailed`). JSON export/import includes settings; CSV is services/accounts only (see [[Import Export]]).

## Dialogs

Confirmations and alerts use `ThemedMessageBoxView` (via `DialogService.Confirm`) — a themed in-app window that follows application light/dark resources, not `System.Windows.MessageBox.Show`.

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

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global hotkeys, and themed confirmation dialogs (`ThemedMessageBoxView`) stay out of the automated suite — [[Testing and CI]].
