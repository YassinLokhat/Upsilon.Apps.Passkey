# MAUI parity checklist

WPF and MAUI are both kept and keep evolving. WPF is the full-featured Windows reference; MAUI (Windows and Android) shares its ViewModels through `GUI/Presentation`, so a feature fixed there reaches both clients.

Status today: every feature below is implemented on MAUI, but the client is still a **preview** until the manual smoke list passes on both platforms. `[x]` means implemented and covered by Presentation tests, `[~]` partial, `n/a` not applicable on that platform.

## Host shell

| Item | Windows | Android |
| ---- | ------- | ------- |
| Progressive login (passkey never TwoWay-bound) | [x] | [x] |
| Open by username (`{hash}.pku` in the default directory) | [x] | [x] (app-private) |
| Open vault file / SAF document | [x] path | [x] SAF document; only the vault in use keeps its persisted grant |
| Working copy + conflict write-back | n/a (direct path) | [~] conflict kept as one copy per session and saved out from the login screen; no keep both / reload / overwrite choice |
| Auto-logout / clipboard clear (wall-clock) | [x] Core | [x] Core |
| Background: clear login fields / half-open session | n/a (kept, like WPF) | [x] |
| Background lock on resume (`BackgroundLockSeconds`) | n/a | [x] |
| Re-mask revealed secrets on background | n/a | [x] |
| Login idle timeout (`LoginIdleTimeoutSeconds`) | [ ] | [ ] |
| `FLAG_SECURE` / screenshot block | n/a | [x] activity + recents (alert windows not covered) |
| Autofill / IME learning disabled on secrets | [~] | [~] login entry only |
| Offline `.pkbf` (build, update, cancel, delete) | [x] | n/a |

## Feature pages (vs WPF)

| Feature | Windows MAUI | Android |
| ------- | ------------ | ------- |
| Services / accounts editor | [x] master-detail, menu bar shortcuts | [x] list then detail, swipe actions, toolbar |
| User settings (create/save/import/export) | [x] WinRT pickers | [x] SAF documents, no temp files |
| App settings | [x] | [x] (no `.pkbf` section) |
| Activities | [x] (no date pickers) | [x] (no date pickers) |
| Alerts detail | [x] | [x] |
| Password generator | [x] | [x] |
| QR | [x] | [x] |
| Credentials confirmation | [x] | [x] |
| Themes Light/Dark/System | [x] | [x] |
| Localization (resx + language picker) | [x] | [x] |
| Keyboard accelerators / copy buttons | [x] | copy buttons [x] |
| Global paste hotkeys | n/a (WPF only) | n/a |

## Smoke (manual)

Adapt the README WPF smoke list:

1. Create or open a vault; login with ordered passkeys; Reset clears a half-open login.
2. Edit an account; autosave survives process kill; merge on next login.
3. Copy password; confirm clipboard auto-clear; on Windows confirm history scrub.
4. Settings: change a passkey (credentials confirmation), rename follows the username hash; export JSON/CSV then re-import.
5. Android: open `.pku` from Drive/OneDrive/Syncthing via SAF; save; confirm write-back; simulate external replace → conflict reported, the source left untouched, the copy saved out from the login screen.
6. Android: background the app mid-login → fields cleared; hold-to-reveal then background → masked; resume after `BackgroundLockSeconds` → vault closed.
7. Windows: build the offline `.pkbf`, close the window mid-build → cancelled within 15 s.

## Shipping MAUI

MAUI stays `ship: false` in `versions.json` until the smoke list passes on Windows and Android. `GUI/WPF` is not retired: both clients remain supported, and WPF keeps the Windows-only features (global paste hotkeys, multi-window).
