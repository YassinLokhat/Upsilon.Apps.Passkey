# MAUI Client

The cross-platform host lives in `GUI/MAUI` (`net10.0-android` + Windows unpackaged). Shared ViewModels and ports live in `GUI/Presentation` (zero NuGet). WPF remains the full-featured Windows reference and stays supported; MAUI covers the same features except the Windows-only ones listed in [[MAUI Parity Checklist]]. Without a picked file, the username resolves to `{hash}.pku` in the default database directory; "Open vault…" picks a file (Windows) or a SAF document (Android) first, like WPF's Ctrl+O.

## Targets

| TFM | Notes |
| --- | ----- |
| `net10.0-windows10.0.19041.0` | `WindowsPackageType=None` (zip-style, like WPF) |
| `net10.0-android` | `minSdk` 29; no offline `.pkbf`; SAF working copy for shared vaults |

iOS / MacCatalyst are out of scope for v1.

## Composition

`MauiProgram` assigns Presentation `AppServices` ports: dispatcher, timers, dialogs, theme, `MauiAppPaths`, `JsonAppSettingsStore` (shared `config.json` model, language and theme applied at startup), `IHostCapabilities` (Android: no `.pkbf`, no multi-window), platform clipboard (WinRT / Android sensitive), vault factory (`PathVaultSourceFactory` on Windows, `AndroidVaultSourceFactory` + SAF on Android), `MauiLauncher`. Login uses `LoginViewModel`: passkeys are read from the Entry and cleared — never TwoWay-bound to the ViewModel.

Tool dialogs (password generator, insert identifier, QR code, credentials confirmation) share their ViewModels with WPF (`GUI/Presentation/ViewModels/Tools`) and are modal pages opened through typed requests (`GeneratePasswordRequest`, `InsertIdentifierRequest`, `ShowQrCodeRequest`, `ConfirmCredentialsRequest`) registered on `MauiDialogService`. Credentials confirmation keeps only HMAC digests of the expected factors; the QR matrix is cleared on expiry or close.

User and application settings share `UserSettingsViewModel`, `PasskeyListViewModel` and `AppSettingsViewModel` (`ViewModels/Settings`). The user settings page asks for the stored credentials before its ViewModel exists, and passkey rows reach the ViewModel only through `ISecretInput`, never a binding. JSON/CSV import and export go through `IFileExchange` streams: WinRT pickers on Windows, SAF documents without a persisted grant on Android, so plaintext never lands in app storage; imports from non-seekable streams stop at the 64 MiB budget. The offline `.pkbf` section (build, update, cancel, delete) only shows when `SupportsOfflineLeakFilter` is set; on Windows, closing the window cancels a running build and waits up to 15 s.

The activity log and the five alert views (account passwords, duplicated passwords, passkey quality, security settings) also share their ViewModels (`ViewModels/Activities`, `ViewModels/Alerts`): filters, sort order and localized filter options live in the ViewModel, and MAUI pages are opened through `ActivitiesRequest` and the `*AlertRequest` types. Shared ViewModels implement `ILanguageRefreshable`; WPF's `ForwardToDataContext` forwards to it. MAUI activity filters have no date pickers yet.

Parity progress: [[MAUI Parity Checklist]]. Today MAUI is a preview: login, the services and accounts editor (master-detail from 800 px wide, list then detail page on phones, swipe actions, Windows menu bar shortcuts, Android toolbar), tools, activities and alerts, user and application settings. It is not shipped (`ship: false`) until the checklist is green. WPF is kept alongside MAUI.

## Android storage

`SafVaultSource` copies a SAF URI into `NoBackupFilesDir`, runs Core on the working path, and write-backs with SHA-256 conflict detection (never silent overwrite). The shared `SessionService` owns the source: write-back runs after Core raises `DatabaseClosed`, one at a time in close order; a conflict keeps a conflict copy, a failed write restores the pristine copy, and the user sees a fixed localized message (never a path or URI). An unchanged working copy skips the write. Closing the window ends the session and waits up to 10 s for pending write-backs. On Android, going to the background calls `ISessionService.FlushToSourceAsync` (Core `FlushPendingChanges`, then a write-back from a snapshot while the session stays open). The working directory is stable per document URI, so edits left by a crash are recovered on the next open when the source is unchanged, or kept as a conflict copy otherwise. Deleting a vault goes through `ISessionService.DeleteVault`, which deletes the SAF document (after re-checking its hash) instead of writing back, and releases the persisted grant. `MainActivity` uses `ACTION_OPEN_DOCUMENT` / `ACTION_CREATE_DOCUMENT` with persistable read/write grants; `CreateInteractiveAsync` refuses a non-empty chosen document. See [[ADR-001-MAUI-Host-and-Multi-Device]].

## Android security posture

- `FLAG_SECURE` on `MainActivity`, recents screenshot disabled on API 33+ (MAUI alert windows are not covered; they never show secrets)
- Background lock: `BackgroundLockPolicy` closes an open vault on resume after `BackgroundLockSeconds` (default 30); revealed passwords and passkeys are re-masked on background (`BackgroundGuard`)
- Conflict copies: one per session (later flushes stop after a conflict), at most five per vault, saved out from the login screen through `IConflictCopyStore` + `IFileExchange`
- SAF grants: only the vault in use keeps its persisted grant; stale grants are released at startup
- `allowBackup=false` + full / device-transfer exclusion XML
- `network_security_config` (no cleartext; system trust anchors only)
- Manifest: `INTERNET` only (no `ACCESS_NETWORK_STATE`)
- Offline Bloom (`.pkbf`) disabled (`OfflineLeakFilterPath` is null)

## Build

```bash
dotnet workload install maui-windows maui-android
dotnet restore Upsilon.Apps.Passkey.MAUI.slnx --locked-mode
dotnet build GUI/MAUI/Upsilon.Apps.Passkey.GUI.MAUI.csproj -f net10.0-windows10.0.19041.0
dotnet build GUI/MAUI/Upsilon.Apps.Passkey.GUI.MAUI.csproj -f net10.0-android
```

CI: `.github/workflows/csharp-dotnet-maui-windows.yml` and `csharp-dotnet-maui-android.yml`.

## Spikes

Manual measurements before Android release: [[Spikes-MAUI]].
