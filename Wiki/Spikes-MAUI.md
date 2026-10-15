# MAUI spikes checklist

These measurements are **manual** (Release + trimming). Record medians on a low-end phone, a mid/high-end phone, and a Windows reference PC. Do **not** lower KDF parameters based on results.

## Performance

1. `CryptographyCenter.GetSlowHash` — 1 000 000 PBKDF2-HMAC-SHA-512 iterations.
2. `Database.CreateAsync` with 1–3 passkeys.
3. `OpenAsync` + progressive `LoginAsync` on a vault with ~500 accounts and ~10 000 activity entries.
4. `RSA.Create(4096)` + one PEM import.

**Decision bands (per passkey stretch):** ≤ 2 s OK with progress UI · 2–5 s warn · > 5 s consider managed SHA-512 stretch (vectors vs BCL) — never change sticky header / defaults.

## Interop

Create on Windows → Open/Login/Save on Android → reopen on Windows (and reverse). Cover both KDF algorithms, autosave present, purged activities, and tamper cases (`ActivityLogTampered`).

## SAF

`ACTION_OPEN_DOCUMENT_TREE` / `ACTION_OPEN_DOCUMENT`, persistable permission, write-back `"wt"`, re-read hash, conflict when Drive/OneDrive/Syncthing changed the file.
