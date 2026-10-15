# MAUI Client

The cross-platform host lives in `GUI/MAUI` (`net10.0-android` + Windows unpackaged). Shared ViewModels and ports live in `GUI/Presentation` (zero NuGet). WPF remains the full-featured Windows reference; MAUI is catching up (login + vault open first). Without a picked file, the username resolves to `{hash}.pku` in the default database directory; "Open vault…" picks a file (Windows) or a SAF document (Android) first, like WPF's Ctrl+O.

## Targets

| TFM | Notes |
| --- | ----- |
| `net10.0-windows10.0.19041.0` | `WindowsPackageType=None` (zip-style, like WPF) |
| `net10.0-android` | `minSdk` 29; no offline `.pkbf`; SAF working copy for shared vaults |

iOS / MacCatalyst are out of scope for v1.

## Composition

`MauiProgram` assigns Presentation `AppServices` ports: dispatcher, timers, dialogs, theme, `MauiAppPaths`, `JsonAppSettingsStore` (shared `config.json` model, language and theme applied at startup), `IHostCapabilities` (Android: no `.pkbf`, no multi-window), platform clipboard (WinRT / Android sensitive), vault factory (`PathVaultSourceFactory` on Windows, `AndroidVaultSourceFactory` + SAF on Android), `MauiLauncher`. Login uses `LoginViewModel`: passkeys are read from the Entry and cleared — never TwoWay-bound to the ViewModel.

Parity progress: [[MAUI Parity Checklist]]. Today MAUI is a preview (login + read-only session overview); it is not shipped (`ship: false`) until the checklist is green. WPF is kept alongside MAUI.

## Android storage

`SafVaultSource` copies a SAF URI into `NoBackupFilesDir`, runs Core on the working path, and write-backs with SHA-256 conflict detection (never silent overwrite). The shared `SessionService` owns the source: write-back runs after Core raises `DatabaseClosed`, one at a time in close order; a conflict keeps a conflict copy, a failed write restores the pristine copy, and the user sees a fixed localized message (never a path or URI). An unchanged working copy skips the write. Closing the window ends the session and waits up to 10 s for pending write-backs. `MainActivity` uses `ACTION_OPEN_DOCUMENT` with persistable read/write grants. See [[ADR-001-MAUI-Host-and-Multi-Device]].

## Android security posture

- `FLAG_SECURE` on `MainActivity`
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
