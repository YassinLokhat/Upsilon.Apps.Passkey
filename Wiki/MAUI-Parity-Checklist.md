# MAUI parity checklist

WPF and MAUI are both kept and keep evolving. Their logic is shared through `GUI/Presentation`, so a fix there reaches both clients. A new feature ships in both hosts, or the gap is listed on this page.

MAUI is a **preview** until the manual smoke list below passes on Windows and Android. `[x]` implemented, `[~]` partial (detail in the cell), `n/a` not applicable on that platform.

## Host shell

| Item | WPF | MAUI Windows | MAUI Android |
| ---- | --- | ------------ | ------------ |
| Progressive login (passkey never TwoWay-bound) | [x] | [x] | [x] |
| New user, App settings, Password generator, Open from the login screen | [x] Ctrl+N, Ctrl+,, Ctrl+P, Ctrl+O | [x] same shortcuts | [x] toolbar and Open button |
| Open by username (`{hash}.pku` in the default directory) | [x] | [x] | [x] app-private directory |
| Open vault file / SAF document | [x] path | [x] path | [x] SAF document; only the vault in use keeps its persisted grant |
| Vault path as first command-line argument | [x] | [x] | n/a |
| Login idle timeout (`LoginIdleTimeoutSeconds`, countdown in the title) | [x] | [x] | [x] |
| Cancel a half-open login | [x] Escape | [x] Reset | [x] Reset |
| Working copy + conflict write-back | n/a (direct path) | n/a (direct path) | [~] one conflict copy per session, saved out from the login screen; no keep both / reload / overwrite choice |
| Auto-logout / clipboard auto-clear | [x] | [x] | [x] the clip is cleared at the deadline or session end even while another app has focus; if Android froze the process, on return |
| Copies kept out of clipboard history, cloud sync and monitors | [x] | [~] history and cloud only | n/a (clip marked sensitive) |
| Background: clear login fields / half-open session | n/a | n/a (minimize keeps them, like WPF) | [x] |
| Background lock on resume (`BackgroundLockSeconds`) | n/a | n/a | [x] |
| Re-mask revealed secrets and close the QR code on background | n/a | n/a | [x] |
| `FLAG_SECURE` / screenshot block | n/a | n/a | [x] activity + recents (alert windows not covered) |
| Autofill and keyboard learning disabled on text fields | n/a | n/a | [x] every entry and editor |
| Offline `.pkbf` (build, update, cancel, delete) | [x] | [x] | n/a |
| Closing during a `.pkbf` build | [x] Yes / No / Cancel prompt | [~] cancels the build and waits up to 15 s | n/a |
| Several top-level windows | [x] | n/a (modal pages) | n/a (modal pages) |

## Feature pages

| Feature | WPF | MAUI Windows | MAUI Android |
| ------- | --- | ------------ | ------------ |
| Services / accounts editor | [x] | [x] master-detail from 800 px wide | [x] list then detail, swipe actions |
| Vault filters (service, identifier type, identifier, text, unsaved only) | [x] | [x] | [x] |
| Save, undo / redo | [x] Ctrl+S, Ctrl+Shift+Z, Ctrl+Shift+Y | [x] same shortcuts | [x] toolbar |
| Password reveal | [x] hold | [x] hold | [x] hold; re-masked on background |
| Password history (copy, QR) | [x] | [x] | [x] |
| User settings (create, save, import, export, delete) | [x] | [x] WinRT pickers | [x] SAF documents, no temp files |
| Credentials confirmation | [x] Escape resets | [x] Reset button | [x] Reset button |
| App settings | [x] | [x] | [x] no `.pkbf` section; background lock delay |
| Activities (dates, event type, search, needs review, copy) | [x] sortable columns | [x] sort picker | [x] sort picker |
| Alerts (menu + five detail views) | [x] | [x] | [x] |
| Password generator, insert identifier | [x] | [x] | [x] |
| QR code (closes after `ShowPasswordDelay`) | [x] | [x] also on logout | [x] also on logout and background |
| Themes Light / Dark / System | [x] | [x] | [x] |
| Localization (resx, live language switch) | [x] | [x] | [x] |
| Copy identifier / password | [x] Ctrl+Shift+L, Ctrl+Shift+P, buttons | [x] same shortcuts, buttons | [x] buttons, swipe actions |
| Keyboard shortcuts | [x] | [x] menu bar accelerators | n/a (toolbar) |

Ctrl+Shift+L / Ctrl+Shift+P copy only; nothing is ever pasted into another application.

## Smoke (manual)

Adapt the README WPF smoke list:

1. Create or open a vault; login with ordered passkeys; Reset clears a half-open login; idle on the login screen until `LoginIdleTimeoutSeconds` clears it.
2. Edit an account; autosave survives process kill; merge on next login.
3. Copy password; confirm clipboard auto-clear; on Windows confirm history scrub.
4. Settings: change a passkey (credentials confirmation), rename follows the username hash; export JSON/CSV then re-import.
5. Android: open `.pku` from Drive/OneDrive/Syncthing via SAF; save; confirm write-back; simulate external replace → conflict reported, the source left untouched, the copy saved out from the login screen.
6. Android: background the app mid-login → fields cleared; hold-to-reveal then background → masked; resume after `BackgroundLockSeconds` → vault closed.
7. Android: copy a password, switch to another app and wait for the clipboard timeout → clip cleared (or on return, if Android froze the app).
8. Windows: build the offline `.pkbf`, close the window mid-build → cancelled within 15 s.

## Shipping MAUI

MAUI stays `ship: false` in `versions.json` until the smoke list passes on Windows and Android. `GUI/WPF` is not retired: both clients remain supported. WPF keeps several top-level windows where MAUI uses modal pages.
