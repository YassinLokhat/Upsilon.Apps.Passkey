# Threat Model

Passkey assumes the `.pku` file may be copied or stolen, and that someone may sit at the login UI. It does **not** assume a trustworthy host once the vault is unlocked.

Authoritative list: [`SECURITY.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/SECURITY.md). Crypto layout: [[Vault Format]]. Reporting: [[Security]].

## In scope

* Confidentiality and integrity of the `.pku` database file **at rest**
* Resistance to **offline** brute-force against the master passkeys
* Friction against **interactive (online)** guessing during progressive login (no rollback of a wrong passkey)
* Tamper **detection** of stored data (AEAD on the onion; seal + watermark on the activity log)
* Limiting exposure of secrets during an **active session** (auto-logout, clipboard cleaning)

## Out of scope

* A compromised host (malware, keylogger, memory scraper, or an attacker with code execution on the machine while the database is unlocked)
* The security of the operating system, its clipboard, and its swap/hibernation files
* Physical access to an unlocked, logged-in session (the WPF **credential confirmation** dialog before create / username-or-passkey save / Delete / opening User Settings is intentionality / anti-mistype only — not a cryptographic step-up; Core still allows writing `IUser.Username` / `IUser.Passkeys` without it — see [[Security]] / [[WPF Client]])
* Plaintext files the user deliberately produces via **Import/Export** (WPF warns before export; backup remains the `.pku`)

## Scenario: stolen `.pku` on a USB stick

The attacker has the ZIP, not the passkeys.

* They can read `header` (algorithm, iteration count, salt). That does not decrypt `database`.
* They can read `activity` seal metadata and the RSA public key, but **not** a cleartext username from that envelope (usernames inside event payloads remain RSA-hybrid encrypted).
* Offline guessing must pay **1,000,000 PBKDF2-HMAC-SHA-512 iterations per passkey** (or whatever the sticky header recorded, still bounded by the KDF floor and ceiling), then peel nested AES-256-GCM layers, starting from `GetHash(username)`.
* A forged `header` with extreme iterations, output length, or salt length is refused before PBKDF2 runs (local DoS hardening).
* A forged archive with a zip bomb, unexpected ZIP entries, or a huge activity list is refused by documented resource budgets before unbounded allocation.
* Unique per-file salt prevents rainbow tables across vaults even when usernames and passkeys are reused.
* AEAD rejects bit flips. The public outer layer distinguishes "not a vault" from "wrong password" without giving a decryption oracle on the real payload.

This is the intended offline story. Weak or few passkeys remain the user's risk.

## Scenario: attacker at the keyboard on the login UI

They do not have a memory dump; they can type guesses.

* Each guess costs a full slow-hash (~one second by design).
* A wrong passkey **poisons the in-memory onion**. They cannot "backspace" the last key; they must close and reopen, then stretch **every** passkey entered so far again.
* Failed attempts can be written to the activity log (public-key hybrid encryption) so the owner sees `LoginFailed` later — unless the attacker also has write access to the file and strips the **unsealed** tail (see next scenario).

This is online friction, not a rate-limit server. It does not replace strong, ordered passkeys.

## Scenario: attacker edits `activity` between sessions

They have write access to the `.pku` (stolen disk, sync conflict, backup restore) but not the passkeys.

* They **cannot** silently rewrite sealed entries from the last logged-in save: login verifies RSA-PSS over the sealed prefix and the watermark inside the AEAD `database` payload. Failure records `ActivityLogTampered` and feeds an `ActivityReview` alert (`IActivityReviewAlert`). Login is not blocked (availability over fail-closed), so the user can still reach their passwords and then inspect the alert.
* They **can** delete or alter the **unsealed tail** (events since last seal), including failed-login records of their own probing. A trusted external log would be required to close that gap; it is out of scope for a purely local tool.

They still cannot read `database` / `autosave` without the passkeys.

## Scenario: vault unlocked, malware on the same PC

Out of scope. The default `ProtectedSecret` (via injected `ISecretMemoryProtector`) shrinks the window (ciphertext in RAM, `***` in logs) but `Reveal()` still produces a `string` for display, copy, QR, and save. Clipboard and screen are OS surfaces. Auto-logout and clipboard timeouts reduce *casual* exposure; they do not stop a scraper with equal privilege to the process.

## Scenario: imported service URL opens via the Windows shell

In scope for the open action. A crafted import could set `file:`, UNC, or a custom protocol and the user might click “open URL”. The client allowlists **absolute `http`/`https` only** before `UseShellExecute`; `http` requires an extra confirmation. Other schemes never reach the shell.

## Scenario: user exports JSON "for backup" to a cloud folder

Out of scope by design. Export is plaintext for interoperability. The user is responsible for protecting or deleting those files. Prefer copying the encrypted `.pku` if the goal is backup — and never let two machines write it at once (`FileShare.Read | FileShare.Delete` plus `.pku.lock` during a session is not multi-master sync).

## Scenario: QR code on a projector or in a screenshot

The identifier or password is in the pixels until the window closes or `ShowPasswordDelay` elapses. Anyone who can see or photograph the screen can capture it. Treat QR reveal like writing the secret on a whiteboard.
