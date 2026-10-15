# MAUI parity checklist

WPF remains the full-featured Windows reference until this list is green on both Windows and Android. **Do not remove `GUI/WPF` until then.**

## Host shell

| Item | Windows | Android |
| ---- | ------- | ------- |
| Progressive login (passkey never TwoWay-bound) | [x] | [x] |
| Open by username (`{hash}.pku`) | [x] | [x] (app-private) |
| Open vault file / SAF document | [x] path | [x] SAF + persistable |
| Working copy + conflict write-back | n/a (direct path) | [x] |
| Auto-logout / clipboard clear (wall-clock) | [x] Core | [x] Core |
| Background: clear login fields / half-open session | [x] | [x] |
| `FLAG_SECURE` / screenshot block | n/a | [x] |
| Autofill / IME learning disabled on secrets | partial | [x] |
| Offline `.pkbf` | [ ] host UI | n/a (v1) |

## Feature pages (vs WPF)

| Feature | Windows MAUI | Android |
| ------- | ------------ | ------- |
| Services / accounts editor | [ ] | [ ] |
| User settings (create/save/import/export) | [ ] | [ ] |
| App settings | [ ] | [ ] |
| Activities | [ ] | [ ] |
| Alerts detail | [ ] | [ ] |
| Password generator | [ ] | [ ] |
| QR | [ ] | [ ] |
| Credentials confirmation | [ ] | [ ] |
| Themes Light/Dark/System | [x] port | [x] port |
| Localization (resx + hot reload) | [ ] | [ ] |
| Keyboard accelerators / copy buttons | [ ] | copy buttons [ ] |

## Smoke (manual)

Adapt the README WPF smoke list:

1. Create or open a vault; login with ordered passkeys; Escape resets poison.
2. Edit an account; autosave survives process kill; merge on next login.
3. Copy password; confirm clipboard auto-clear; on Windows confirm history scrub.
4. Android: open `.pku` from Drive/OneDrive/Syncthing via SAF; save; confirm write-back; simulate external replace → conflict (keep both / reload / overwrite).
5. Background the app mid-login → fields cleared; resume after session expiry → locked.

## Retirement of WPF

When Windows MAUI matches the feature table above (except documented exceptions) and Android matches except `.pkbf` + hotkeys, remove `GUI/WPF` from solutions, CI, and docs in a dedicated PR.
