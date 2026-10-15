# ADR-001: MAUI host NuGet exception and multi-device vault model

**Status:** Accepted  
**Date:** 2026-10-15  
**Context:** Migration of the WPF client to .NET MAUI (Windows + Android) while keeping Core / Utils / Interfaces zero-NuGet and opening the same `.pku` files on both platforms.

## Decision 1 — NuGet allowlist scoped to the MAUI host

- `Interfaces`, `Utils`, `Core`, `GUI/Presentation`, and `GUI/WPF` keep `EnforceZeroExternalDependencies` with **no** third-party `PackageReference`.
- Only `GUI/MAUI` may set `AllowedExternalPackagePrefixes` (today: `Microsoft.Maui.`). Direct allowlisted package: `Microsoft.Maui.Controls`.
- Transitive packages are accepted as the MAUI SDK surface: Microsoft packages (Windows App SDK, WebView2, Win2D) and Microsoft-published .NET bindings of AndroidX, Google, Kotlin and JetBrains libraries. They are locked via `packages.lock.json` + `--locked-mode` in CI, audited with `dotnet list … package --vulnerable`, and restored through `nuget.config` (nuget.org only, source mapping, `signatureValidationMode=require`). The trusted nuget.org repository signer has no `<owners>` list, so the signature check accepts any nuget.org package; the lock file's content hashes are what pin the set.
- Rationale: MAUI cannot ship without the Microsoft UI stack; widening the allowlist to CommunityToolkit would grow the supply-chain surface without unlocking Core.

## Decision 2 — Multi-device access without sync protocol

Passkey remains **local-only**: there is no server-side merge. Opening the same `.pku` on Windows and Android (via copy or a sync folder) is supported with host-side conflict detection:

1. **One writer at a time.** Close the vault and wait for the sync client before opening elsewhere.
2. **Android:** SAF document (`ACTION_OPEN_DOCUMENT`) + persisted URI + private working copy under `NoBackupFilesDir`. Core keeps path-based `FileLocker` I/O on the working copy. Before write-back, re-hash the source; on mismatch, never overwrite silently. Today the write-back is refused, the working copy is kept locally and the user is told; an explicit keep both / reload / overwrite choice is planned but not implemented. SAF tree mode (`ACTION_OPEN_DOCUMENT_TREE`) is not implemented either.
3. **Windows:** detect external replace of the live `.pku` before atomic commit (session handle can retain the pre-replace inode); surface a conflict instead of silent overwrite.
4. **No ZIP-entry merge** across copies (activity seal / autosave are not mergeable).
5. **Offline `.pkbf`:** Windows only; Android uses online HIBP / XposedOrNot only.

## Spikes (manual — run on devices before Android release)

Not run yet: no measurement has been recorded (see [[Spikes-MAUI]]).

| Spike | Success criteria |
| ----- | ---------------- |
| PBKDF2 / Login / RSA on low-end Android (Release + trim) | ≤ 2 s per passkey preferred; never lower KDF header / defaults |
| Interop `.pku` Windows ↔ Android | Create / Open / Login / Save round-trip under trimming |
| SAF write-back | Drive / OneDrive / Syncthing: `"wt"` + re-read hash |

## Consequences

- New solution `Upsilon.Apps.Passkey.MAUI.slnx` and dedicated CI workflows.
- Docs (`SECURITY.md`, `Threat-Model`, client wiki) must document SecureString loss, Android clipboard limits, and multi-device conflict behaviour.
- Presentation layer (`GUI/Presentation`, net10.0) becomes the shared ViewModel / services home for WPF and MAUI.

## Amendment (2026-10): WPF and MAUI coexist

- `GUI/WPF` is kept permanently; it is not retired once MAUI reaches parity. Both clients ship from the same repository and open the same `.pku` files.
- Shared behaviour lives in `GUI/Presentation` (ViewModels, typed view requests, ports such as `IFileExchange`, `IConflictCopyStore`, `IAppLifecycle`). Hosts only map requests to views and implement ports.
- Desktop-only features stay in the Windows hosts: several top-level windows are WPF-only, the offline `.pkbf` and keyboard shortcuts are in WPF and MAUI Windows. Android-only protections stay in the MAUI Android host (SAF working copy, background lock, `FLAG_SECURE`).
- MAUI ships once the manual smoke list in [[MAUI Parity Checklist]] passes; until then `versions.json` keeps `ship: false` for it.
