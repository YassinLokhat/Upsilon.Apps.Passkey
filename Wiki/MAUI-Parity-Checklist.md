# MAUI parity checklist

WPF and MAUI are both kept and keep evolving: WPF is the full-featured Windows reference, and this list tracks what MAUI (Windows and Android) still lacks.

Status today: the MAUI client is a **preview** that covers login and a read-only session overview only. `[~]` marks a partial item.

## Host shell

| Item | Windows | Android |
| ---- | ------- | ------- |
| Progressive login (passkey never TwoWay-bound) | [x] | [x] |
| Open by username (`{hash}.pku` in the default directory) | [x] | [x] (app-private) |
| Open vault file / SAF document | [x] path | [x] SAF document + persistable grant (no tree mode) |
| Working copy + conflict write-back | n/a (direct path) | [~] conflict detected, write-back refused; no keep both / reload / overwrite choice |
| Auto-logout / clipboard clear (wall-clock) | [x] Core | [x] Core |
| Background: clear login fields / half-open session | n/a (kept, like WPF) | [x] |
| Login idle timeout (`LoginIdleTimeoutSeconds`) | [ ] | [ ] |
| `FLAG_SECURE` / screenshot block | n/a | [x] |
| Autofill / IME learning disabled on secrets | [~] | [x] |
| Offline `.pkbf` | [ ] host UI | n/a (v1) |

## Feature pages (vs WPF)

| Feature | Windows MAUI | Android |
| ------- | ------------ | ------- |
| Services / accounts editor | [ ] (read-only service list only) | [ ] (read-only service list only) |
| User settings (create/save/import/export) | [ ] | [ ] |
| App settings | [ ] | [ ] |
| Activities | [ ] | [ ] |
| Alerts detail | [ ] | [ ] |
| Password generator | [ ] | [ ] |
| QR | [ ] | [ ] |
| Credentials confirmation | [ ] | [ ] |
| Themes Light/Dark/System | [~] port only, no setting UI | [~] port only, no setting UI |
| Localization (resx + hot reload) | [~] (shared resx, follows the OS language; no language picker yet) | [~] (shared resx, follows the OS language; no language picker yet) |
| Keyboard accelerators / copy buttons | [ ] | copy buttons [ ] |

## Smoke (manual)

Adapt the README WPF smoke list:

1. Create or open a vault; login with ordered passkeys; Escape resets poison.
2. Edit an account; autosave survives process kill; merge on next login.
3. Copy password; confirm clipboard auto-clear; on Windows confirm history scrub.
4. Android: open `.pku` from Drive/OneDrive/Syncthing via SAF; save; confirm write-back; simulate external replace → conflict reported and the source left untouched.
5. Background the app mid-login → fields cleared; resume after session expiry → locked.

## Shipping MAUI

MAUI stays `ship: false` in `versions.json` until Windows MAUI matches the feature table above (except documented exceptions) and Android matches except `.pkbf` + hotkeys. `GUI/WPF` is not retired: both clients remain supported.
