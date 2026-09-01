# MAUI Client

The cross-platform client lives in `GUI/MAUI`. It targets **Windows** and **Android** (no iOS in this phase). The WPF client under `GUI/WPF` remains the full-featured Windows legacy UI and is not removed.

Target frameworks: `net10.0-windows10.0.19041.0` and `net10.0-android`.

## Feature parity (current)

Included (vault-compatible with WPF `.pku` files):

* Create / open vault (username + progressive ordered passkeys)
* Autosave merge prompt (Yes / No / Cancel → Core `AutoSaveMergeBehavior`)
* Session + idle auto-logout; clipboard clear on logout
* Services / accounts CRUD (name, URL, notes, identifiers, password)
* Account options: warn if leaked / duplicated, password-update reminder delay
* Password history (copy prior passwords)
* Filter services/accounts + “changed only”
* Warnings banner on Services; Warnings page with **Go to account**
* Copy identifier / password with auto-clear; QR for identifier / password
* Password generator (optional leak check)
* Activities (search + needs-review)
* User settings: timeouts, notify flags, language/theme override, import/export JSON/CSV
* App settings: language/theme, default vault directory, offline HIBP Bloom build/update/delete
* Themes Light / Dark / System; localization EN / FR

### Windows-only (gated)

* Global paste hotkeys (`Ctrl+Shift+L` / `Ctrl+Shift+P`) → copy + synthesize Ctrl+V
* Clipboard write via WinRT `SetContentWithOptions` (excluded from history / cloud)
* Clipboard history scrub (`RemoveAllOccurrenceAsync`)

Android uses MAUI clipboard + auto-clear only (no history API / no global hotkeys). Vault default path is under `FileSystem.AppDataDirectory`; pickers still allow import/export and alternate `.pku` locations.

### Still thinner than WPF

* Per-identifier reorder / QR from insert dialog (insert page adds one identifier with autocomplete)
* Rich date-range filters on activities (event-type filter is available)
* Visible-password toggle in account editor (QR delay setting is honored on QR page)

## Architecture

Same Core stack as WPF. The MAUI project references `Core` only and supplies:

* `AppServices` locator (`IDialogService`, `ISessionService`, `INavigationService`, Utils crypto/JSON/password factory, `IClipboardManager`)
* Platform clipboard partials under `Platforms/Windows` and `Platforms/Android`
* Paths: Windows beside the exe (`raw/`, `config.json`); Android under app data (`Helpers/AppPaths.cs`)

## Prerequisites

* .NET 10 SDK
* Workloads: `maui-windows`, `android` (and Android SDK / emulator for device runs)

```bash
dotnet workload install maui-windows android
```

## Build and run

```bash
# Windows
dotnet build GUI/MAUI -f net10.0-windows10.0.19041.0
dotnet run --project GUI/MAUI -f net10.0-windows10.0.19041.0

# Android (emulator or device)
dotnet build GUI/MAUI -f net10.0-android
dotnet build GUI/MAUI -t:Run -f net10.0-android
```

The Windows solution (`Upsilon.Apps.Passkey.Windows.slnx`) includes the MAUI project alongside WPF.

## Publish / export

```bash
# Windows unpackaged (self-contained folder)
dotnet publish GUI/MAUI -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:PublishReadyToRun=true

# Android APK
dotnet publish GUI/MAUI -f net10.0-android -c Release -p:AndroidPackageFormat=apk

# Android AAB (Play Store)
dotnet publish GUI/MAUI -f net10.0-android -c Release -p:AndroidPackageFormat=aab
```

Outputs land under `GUI/MAUI/bin/Release/<tfm>/publish/` (exact subfolders vary by RID / packaging).

## First vault

1. Run the MAUI app.
2. **New user** → username + at least one ordered passkey → confirm default vault path (or accept the Android app-data path).
3. After create you land on **Services**; add services/accounts or import from User settings.
4. Log out, then open the same vault: **Open vault** (or type username so the default hashed path resolves) and enter each passkey in order.

Vault format is identical to WPF (`.pku`); files are interchangeable across clients.
