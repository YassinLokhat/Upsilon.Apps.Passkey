# Security

Upsilon.Apps.Passkey is a **local-only** password manager. Security is the core feature. This page is the wiki view of [`SECURITY.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/SECURITY.md); if they drift, treat the repository file as authoritative for supported versions and reporting SLAs.

Related: [[Vault Format]], [[Threat Model]], [[Warnings and Activity]].

## Supported versions

Each component is versioned **independently**. Security fixes apply to the latest released version of each component only.

| Component (assembly) | Supported version | Supported |
| -------------------- | ----------------- | --------- |
| `Upsilon.Apps.Passkey.GUI.WPF` | 2.0.x | Yes |
| `Upsilon.Apps.Passkey.Core` | 2.0.x | Yes |
| `Upsilon.Apps.Passkey.Interfaces` | 2.0.x | Yes |

Any version older than the latest release of a given component is not supported. The shared 2.0.x line at the time of writing is not guaranteed to remain aligned.

## Reporting a vulnerability

**Do not open a public GitHub issue.**

* **GitHub Security Advisories** (preferred): [Report a vulnerability](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/security/advisories)
* **Email:** contact@yassinlokhat.com

Include when possible: impact, step-by-step reproduction or a proof of concept, affected component and version, suggested remediation.

What to expect:

* **Acknowledgement** within 7 days
* **Initial assessment** within 14 days (accepted, needs more information, or declined with a rationale)
* **Fix and disclosure:** accepted reports are fixed on a private branch and released as soon as reasonably possible. Coordinated disclosure is preferred; credit is given to reporters who wish to be acknowledged

Non-security bugs: public [GitHub issues](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/issues).

## Supply chain

All cryptography is implemented in `Core/Utils/CryptographyCenter.cs` on the .NET BCL. **Core and Interfaces refuse any third-party NuGet package at build time.** GitHub CodeQL (`security-and-quality`) scans a Release build of production projects (tests excluded) on every push/PR (any branch) and weekly. The query pack is not a NuGet dependency of Core.

All security-relevant randomness uses `System.Security.Cryptography.RandomNumberGenerator` (keys, salts, nonces, generated passwords via `GetInt32`). `System.Random` is never used for secrets.

## At rest

Ordered master passkeys form an AES-256-GCM onion (HKDF-SHA256 per layer) after PBKDF2-HMAC-SHA-512 stretching. Username hash is an implicit first layer. Sticky KDF header + KDF floor: [[Vault Format]].

The activity log uses RSA-4096 hybrid encryption plus a login-time seal: [[Warnings and Activity]]. The activity ZIP envelope does not store a cleartext username.

## In memory

Once unlocked, account passwords, password history, master passkeys, and the RSA private key are held as AES-256-GCM ciphertext under a random, process-wide session key (`Core/Utils/ProtectedSecret.cs`). Plaintext exists only for the duration of `Reveal()` (display, copy, re-encrypt, or JSON persist into the `.pku` onion). `ToString()` never returns the secret (`***`), so a protected value cannot leak into logs or activity messages by accident.

The session key never leaves RAM and dies with the process; a dump of the wrapped blobs after exit is worthless. Persistence still stores plaintext JSON **inside** the onion-encrypted `database` / `autosave` entries — `ProtectedSecret` is an in-memory wrapping, not a second at-rest scheme.

`IDatabase.Login` takes a plain `string` passkey (no `SecureString` overload on Core). The WPF GUI zeroes the BSTR around that call — [[WPF Client]].

Derived AES keys, per-layer UTF-8 password bytes, GCM plaintext buffers, and `ProtectedSecret` unwrap buffers are wiped with `CryptographicOperations.ZeroMemory` after use.

## Progressive login without rollback

Logging in is **progressive**: each `Login` appends one stretched passkey and attempts decryption. There is **no rollback**. A mistype permanently poisons the current open session until `Close` + `Open`.

Combined with the expensive PBKDF2 stretch on every attempt, an interactive guessing loop becomes high-friction: each wrong guess costs a full slow-hash **and** forces a full reopen (and a fresh stretch of every passkey entered so far) before the attacker can try again. The legitimate user who mistypes must restart the sequence. The GUI cancels with Escape for that reason.

## Session protection

* **Auto-logout** after `ISettings.LogoutTimeout` minutes of inactivity; the file handle is released.
* **Clipboard cleaning** after `CleaningClipboardTimeout` seconds, including OS clipboard history via `IClipboardManager`. Paste hotkeys use the same path.

## Password hygiene

* Strong generation: CSPRNG over a configurable alphabet. Leak-checked generation retries at most **five** candidates, then returns empty.
* Leak detection: two free, no-account **k-anonymity** providers (HIBP then XposedOrNot). Only hash prefixes leave the device. Timeouts of a few seconds. Process-local cache of successful answers only. Fail open if both providers are unreachable.
* Duplicate-password and password-expiry warnings are local.

These leak-check HTTP calls are the **only** outbound network the application makes. The feature is opt-in per account.

## Known limitations

These are conscious trade-offs:

* **Secrets in managed memory.** `Reveal()` still returns a .NET `string`, which is immutable and cannot be reliably zeroed before GC. An attacker who can read process memory or the OS swap file while the database is unlocked — especially during display, clipboard copy, QR encoding, or a save — may recover secrets. Consistent with "compromised host" out of scope.
* **On-screen QR codes.** The secret is on the display until the window closes or `ShowPasswordDelay` elapses.
* **PBKDF2 rather than Argon2id.** Argon2 is not in the BCL; Core stays zero-dependency. Compensation: PBKDF2-HMAC-SHA-512 with 1,000,000 iterations. The sticky KDF header keeps the door open to a memory-hard KDF later if the policy is ever relaxed.
* **Import/export files** are plaintext by design for interoperability.
* **Leak check fails open.** Residual risk: a prolonged outage of *both* providers while a breached password stays unmarked. The two corpora are not identical, so a password known only to the provider that is down may be missed.
* **Unsealed activity-log tail.** An attacker with write access to the file can erase unsealed events (including their own failed logins) before the legitimate user logs in again. Sealed prefix + watermark still detect rollback of what was sealed.
* **No login-attempt rollback.** Raises the cost of interactive guessing at the expense of UX.
* **Compressed size leakage.** GZip-before-encrypt means ciphertext length tracks compressed plaintext size. Absolute lengths are already visible for any AEAD ciphertext; this is accepted for storage savings on structured data.
