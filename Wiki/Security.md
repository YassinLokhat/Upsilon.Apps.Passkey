# Security

Upsilon.Apps.Passkey is a **local-only** password manager. Security is the core feature. This page is the wiki view of [`SECURITY.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/SECURITY.md); if they drift, treat the repository file as authoritative for supported versions and reporting SLAs.

Related: [[Vault Format]], [[Threat Model]], [[Alerts and Activity]].

## Supported versions

Each component is versioned **independently**. Security fixes apply to the latest released version of each component only.

<!-- BEGIN:versions-summary -->At the time of writing, `Upsilon.Apps.Passkey.Utils` is on **2.0.x**; `Upsilon.Apps.Passkey.GUI.WPF`, `Upsilon.Apps.Passkey.Core` and `Upsilon.Apps.Passkey.Interfaces` are on **2.1.x**. Each assembly is versioned independently and may diverge.<!-- END:versions-summary -->

<!-- BEGIN:versions-supported-table -->
| Component (assembly) | Supported version | Supported |
| -------------------- | ----------------- | --------- |
| `Upsilon.Apps.Passkey.GUI.WPF` | 2.1.x | Yes |
| `Upsilon.Apps.Passkey.Core` | 2.1.x | Yes |
| `Upsilon.Apps.Passkey.Utils` | 2.0.x | Yes |
| `Upsilon.Apps.Passkey.Interfaces` | 2.1.x | Yes |
<!-- END:versions-supported-table -->

Any version older than the latest release of a given component is not supported. Source of truth: [`versions.json`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/versions.json). The MAUI client (`Upsilon.Apps.Passkey.GUI.MAUI`, Windows and Android) is a preview with no release yet, so it has no supported version; report its vulnerabilities through the same private channels.

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

All cryptography is implemented in `Utils/CryptographyCenter.cs` on the .NET BCL. **Core, Utils, Interfaces, Presentation, and the WPF GUI refuse any third-party NuGet package at build time.** The MAUI host may reference only `Microsoft.Maui.*` directly; its transitive Windows App SDK and AndroidX packages are pinned by `packages.lock.json`, restored in locked mode and audited for known vulnerabilities in CI ([[ADR-001-MAUI-Host-and-Multi-Device]]). GitHub CodeQL scans a Release build of production projects (tests excluded) on every push (any branch) and weekly. CodeQL is not a NuGet dependency of those libraries.

All security-relevant randomness uses `System.Security.Cryptography.RandomNumberGenerator` (keys, salts, nonces, generated passwords via `GetInt32`). `System.Random` is never used for secrets.

## At rest

Ordered master passkeys form an AES-256-GCM onion (HKDF-SHA256 per layer) after PBKDF2-HMAC-SHA-512 stretching. Username hash is an implicit first layer. Sticky KDF header + KDF floor: [[Vault Format]].

The activity log uses RSA-4096 hybrid encryption plus a login-time seal: [[Alerts and Activity]]. The activity ZIP envelope does not store a cleartext username.

## In memory

Once unlocked, account passwords, password history, master passkeys, and the RSA private key are held as `IProtectedSecret` values produced by the `ISecretMemoryProtector` injected at Create/Open. The Utils default (`SecretMemoryProtector` → `ProtectedSecret`) wraps them as AES-256-GCM ciphertext under a random, process-wide session key. Plaintext exists only for the duration of `Reveal()` (display, copy, re-encrypt, or JSON persist into the `.pku` onion). `ToString()` never returns the secret (`***`), so a protected value cannot leak into logs or activity messages by accident.

The session key never leaves RAM and dies with the process; a dump of the wrapped blobs after exit is worthless. Persistence still stores plaintext JSON **inside** the onion-encrypted `database` / `autosave` entries — in-memory wrapping is not a second at-rest scheme. Core depends only on the Interfaces ports; hosts may substitute another `ISecretMemoryProtector` without changing Core.

Session **Undo/Redo** (`IEditHistory`) keeps a capped in-memory stack of autosave-shaped JSON deltas (may include password/account blobs as plaintext strings) until `Close` / auto-logout. It is never written to the `.pku`. Username and master passkeys are excluded. UI/activity must not surface Old/New values.

`IDatabase.Login` takes a plain `string` passkey (no `SecureString` overload on Core). The WPF GUI copies it out of `SecurePassword` and zeroes the BSTR before `LoginAsync` runs on the managed copy — [[WPF Client]]. The MAUI host reads the Entry once and clears it — [[MAUI Client]].

Owned sensitive buffers are wiped with `CryptographicOperations.ZeroMemory` after use: derived AES keys, per-layer UTF-8 HKDF IKM bytes, PBKDF2 buffers in `GetSlowHash`, abandoned onion interlayer plaintext, encrypt-side ciphertext scratch buffers, and `ProtectedSecret` unwrap buffers. Final decrypted UTF-8 strings and other managed password strings are not wiped (see Known limitations).

## Progressive login without rollback

Logging in is **progressive**: each `Login` appends one stretched passkey and attempts decryption. There is **no rollback**. A mistype permanently poisons the current open session until `Close` + `Open`.

Combined with the expensive PBKDF2 stretch on every attempt, an interactive guessing loop becomes high-friction: each wrong guess costs a full slow-hash **and** forces a full reopen (and a fresh stretch of every passkey entered so far) before the attacker can try again. The legitimate user who mistypes must restart the sequence. For that reason WPF cancels with Escape and MAUI with its Reset button.

## Session protection

* **Auto-logout** after `ISettings.LogoutTimeout` minutes of inactivity; the file handle is released. Core counts against a wall-clock deadline, so time spent with an Android process frozen still counts.
* **Clipboard cleaning** after `CleaningClipboardTimeout` seconds, including OS clipboard history via `IClipboardManager` (a no-op on Android, which has no history API). History scrub targets only secrets registered through `IUser.RememberClipboardSecret` (the shared session service records auto-cleared copies); it does not reveal every stored password on each tick. The Windows clients' copy shortcuts (Ctrl+Shift+L / Ctrl+Shift+P) use the same clipboard path and never paste.
* **Service URL storage (Core)**: absolute URIs that are not `http`/`https` are refused (`ServiceUrlHelper`). Relative values may still be stored while typing; shell open re-validates.
* **Credential confirmation (both clients)**: create vault, or save a username/passkey change, asks the user to re-type credentials (shared `CredentialsConfirmationViewModel`, which keeps the expected factors only as HMAC-SHA256 digests). Updates that change credentials require **old then new**. Vault **Delete** and opening **User Settings** require **old** credentials. **Export** does **not** re-prompt for credentials (they were already confirmed to open User Settings) but shows an explicit **plaintext warning** before writing JSON/CSV. Progressive entry **without rollback**, deliberately matching login: a mistype **intentionally poisons** the in-dialog sequence until Escape (WPF) or Reset (MAUI) (not a dialog bug). Closing the dialog cancels the action. WPF passkey input uses `SecurePassword` / `UseAsString` like login; MAUI reads the Entry once and clears it. Host-side intentionality / anti-mistype — not a crypto step-up; Core still allows writing `IUser.Username` / `IUser.Passkeys` without it. Details: [[WPF Client]], [[MAUI Client]].

## Android (MAUI preview)

* **Vault documents**: a `.pku` picked through the system file picker (SAF) is copied to app-private, non-backed-up storage; Core works on that copy and the write-back re-hashes the source first, so an external change is never overwritten silently (one conflict copy per session instead). App backup is disabled.
* **Background**: half-open login fields are cleared, revealed secrets are masked again and the QR code page closes as soon as the app leaves the foreground; on return after `BackgroundLockSeconds` (default 30) the open vault is closed at once. Windows has no background lock.
* **Screen**: `FLAG_SECURE` blocks screenshots and the recents thumbnail of the activity; MAUI alert windows are not covered and never show a secret.
* **Keyboards and autofill**: every text field opts out of autofill and personalized keyboard learning; a keyboard that ignores those flags is out of scope.
* **Clipboard**: copies are marked sensitive. The clip is cleared at the auto-clear deadline and at session end; without focus Android hides it, so it is cleared even if another app copied something since. It stays in place when the app goes to the background.
* **No offline `.pkbf`**: leak checks use the two online providers only.

Details and residual risks: [[MAUI Client]] and the Known Limitations of [`SECURITY.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/SECURITY.md).

## Password hygiene

* Strong generation: CSPRNG over a configurable alphabet. Leak-checked generation retries at most **five** candidates, then returns empty.
* Leak detection: two free, no-account **k-anonymity** providers (HIBP then XposedOrNot), then an optional machine-local HIBP Bloom filter (`.pkbf`). Only hash prefixes leave the device for the remote calls. Timeouts of a few seconds. Process-local cache of successful answers only. Order: HIBP → XposedOrNot → Bloom (if enabled and present) → fail-open. Filter path is **fixed by the host** (not in `config.json`): `pwned-ntlm.pkbf` next to the executable when that folder is writable, otherwise in the per-user app data folder (WPF and MAUI Windows; Android has no offline filter); sidecar `<filter>.pkbf.ranges` for incremental updates. A Bloom **miss** is definitive "not leaked"; a **hit** is treated as leaked (false positives at the build-time quality target; default Balanced ~1 %, no false negatives). Quality preset (`LocalLeakDatabaseQuality` / `LeakFilterConfig.FalsePositiveRate`: Balanced / Strict / Paranoid) applies only on full build or rebuild; updates keep on-disk sizing. Enabling/disabling never deletes the file; only an explicit delete removes it (and the sidecar). Build / update / enable / delete from **App Settings** (`Ctrl+,`) in WPF or MAUI Windows, or `HibpBloomBuilder.RunAsync`. Auto-update interval is `LeakFilterConfig.AutoUpdateFrequency` days (app setting `LocalLeakDatabaseAutoUpdateFrequency`, default **7**; **0** = off): when offline use is enabled and an existing `.pkbf` + `.ranges` sidecar are present and older than that interval (by header `BuiltUtc`), the Windows clients refresh in the background at startup; it never starts a first full build, a missing sidecar skips the refresh while keeping the filter, and a corrupt `.pkbf` throws `HibpBloomCorruptException` (auto-update logs and skips — only an explicit Rebuild replaces it). If the UI continues a mid-close refresh in the background, the vault session (and owned clipboard) is ended first. Implementation: `Utils/LeakFilter/`. UX when closing mid-job: [[WPF Client]].
* Duplicate-password and password-update reminder alerts are local.

These leak-check HTTP calls are the **only** outbound network the application makes. The feature is opt-in per account. The offline filter is **application-scoped** (shared by all vaults; not stored in the `.pku`).

## Known limitations

These are conscious trade-offs:

* **Secrets in managed memory.** `Reveal()` still returns a .NET `string`, which is immutable and cannot be reliably zeroed before GC. An attacker who can read process memory or the OS swap file while the database is unlocked — especially during display, clipboard copy, QR encoding, or a save — may recover secrets. Consistent with "compromised host" out of scope.
* **On-screen QR codes.** The secret is on the display until the window closes or `ShowPasswordDelay` elapses (MAUI also closes it on logout and, on Android, when the app goes to the background).
* **PBKDF2 rather than Argon2id.** Argon2 is not in the BCL; Core, Utils, Interfaces, Presentation, and WPF stay zero-dependency. Compensation: PBKDF2-HMAC-SHA-512 with 1,000,000 iterations. The sticky KDF header keeps the door open to a memory-hard KDF later if the policy is ever relaxed.
* **Import/export files** are plaintext by design for interoperability. JSON import **refuses** settings that would set `LogoutTimeout`, `CleaningClipboardTimeout`, or `ShowPasswordDelay` to `0` (`ImportExportError.SecurityTimeoutsDisabled`); those controls may still be disabled deliberately in the UI.
* **Leak check fails open** when both remotes are unreachable **and** no offline Bloom filter is attached (absent or disabled via `LeakFilterConfig`). Residual risk without a local filter: a prolonged outage of *both* providers while a breached password stays unmarked. The two remote corpora are not identical, so a password known only to the provider that is down may be missed. When a filter *is* attached, Bloom hits may include false positives at the build-time quality target (default ~1 %).
* **Leak API response authenticity.** Successful HTTP 200 bodies are not authenticated beyond TLS. Empty HIBP ranges are not cached (fall through); a full Bloom build with zero hashes is refused. Residual MITM risk for forged non-empty "clean" answers remains (documented in repository `SECURITY.md`).
* **Unsigned offline Bloom (`.pkbf`).** Format checks only — no HMAC/signature. Local file replacement can induce false negatives; overlaps host compromise.
* **Offline Bloom filter size / freshness.** A full HIBP-derived `.pkbf` takes hours to build; size depends on quality (~2.4 / ~3.5 / ~4.7 GiB). Changing quality needs an explicit rebuild — `Update` never changes sizing. Updates are incremental (concurrent `If-None-Match` against the `.pkbf.ranges` sidecar; default parallelism 64). The sidecar is a cache bound to one committed filter state and is rejected when that no longer matches. With a non-zero `AutoUpdateFrequency`, the WPF host can refresh an existing file at startup when the sidecar is present and `BuiltUtc` is stale; absence of the file (or of the sidecar) never triggers an automatic first full download. A corrupt / unusable `.pkbf` on Update throws `HibpBloomCorruptException` (auto-update logs and skips) — only an explicit **Rebuild** replaces the file.
* **Unsealed activity-log tail.** An attacker with write access to the file can erase unsealed events (including their own failed logins) before the legitimate user logs in again. Sealed prefix + watermark still detect rollback of what was sealed.
* **No login-attempt rollback.** Raises the cost of interactive guessing at the expense of UX.
* **Compressed size leakage.** GZip-before-encrypt means ciphertext length tracks compressed plaintext size. Absolute lengths are already visible for any AEAD ciphertext; this is accepted for storage savings on structured data.
