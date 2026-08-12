# Security Policy

Upsilon.Apps.Passkey is a **local-only** password manager. There is no server, no
account, and no synchronization: every secret lives inside a single encrypted
`.pku` file on the user's device. Security is therefore the core feature of this
project, and this document describes exactly how the current implementation
protects data and how to report a problem.

## Supported Versions

Each component is versioned **independently** and may evolve at its own pace.
Security fixes are applied to the latest released version of each component only,
so please always upgrade to the most recent release before reporting an issue.
The components happen to share version 2.0.x at the time of writing, but this is
not guaranteed to remain the case.

| Component (assembly)                  | Supported version | Supported          |
| ------------------------------------- | ----------------- | ------------------ |
| `Upsilon.Apps.Passkey.GUI.WPF` (app)  | 2.0.x             | :white_check_mark: |
| `Upsilon.Apps.Passkey.Core`           | 2.0.x             | :white_check_mark: |
| `Upsilon.Apps.Passkey.Interfaces`     | 2.0.x             | :white_check_mark: |

Any version older than the latest release of a given component is not supported.

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Report vulnerabilities privately through either of the following channels:

- **GitHub Security Advisories** (preferred):
  1. Go to the repository's **Security** tab:
     <https://github.com/YassinLokhat/Upsilon.Apps.Passkey/security/advisories>
  2. Click **Report a vulnerability** and describe the issue.
- **Email**: <contact@yassinlokhat.com>

Please include, when possible:

- A description of the vulnerability and its impact.
- Step-by-step reproduction instructions or a proof of concept.
- The affected component and version (see the table above).
- Any suggested remediation.

What to expect:

- **Acknowledgement**: within 7 days.
- **Initial assessment**: within 14 days, including whether the report is
  accepted, needs more information, or is declined (with a rationale).
- **Fix and disclosure**: for accepted reports, a fix is prepared on a private
  branch and released as soon as reasonably possible. Coordinated disclosure is
  preferred; credit is given to reporters who wish to be acknowledged.

## Threat Model

**In scope**

- Confidentiality and integrity of the `.pku` database file at rest.
- Resistance to offline brute-force against the master passkeys.
- Friction against interactive (online) guessing during progressive login
  (no rollback of a wrong passkey; see "Progressive login without rollback").
- Tamper detection of stored data.
- Limiting exposure of secrets during an active session (auto-logout, clipboard
  cleaning).

**Out of scope**

- A compromised host (malware, keylogger, memory scraper, or an attacker with
  code execution on the machine while the database is unlocked).
- The security of the operating system, its clipboard, and its swap/hibernation
  files.
- Physical access to an unlocked, logged-in session.
- Plaintext files the user deliberately produces via **Import/Export** (see
  "Known Limitations").

## Security Design

All cryptography is implemented in `Core/Utils/CryptographyCenter.cs` on top of
`System.Security.Cryptography` (the .NET BCL). **The project has a strict
zero-external-dependency policy for the `Core` and `Interfaces` libraries**: no
third-party cryptographic package is used, which keeps the security-critical
supply-chain attack surface minimal.

### Master passkeys (multi-factor "onion")

- A database is protected by an **ordered set of passkeys** (master passwords).
  All of them, **in the correct order**, are required to decrypt the data.
- The onion always starts with an **implicit first layer keyed by the username**:
  `GetHash(username)` (fast SHA-512, Base64 with `/` replaced by `-`) is pushed
  onto the stack before any stretched passkey. Changing the username therefore
  changes the ciphertext layout; it is not a secret factor on its own, but it is
  part of the key material used at rest.
- Each passkey is stretched with **PBKDF2-HMAC-SHA-512, 1,000,000 iterations**
  (64-byte output) before being used as key material (`GetSlowHash`). This far
  exceeds the OWASP baseline and makes offline guessing expensive. HMAC-SHA-512
  is used deliberately: its 64-bit arithmetic is markedly less efficient on the
  GPUs and ASICs an attacker would use for parallel guessing than the 32-bit
  operations of SHA-256.
- The PBKDF2 salt is a **random 128-bit value generated once when the database
  is created** and stored in the file's `header` entry. It is stable for the life
  of the file (required to reopen it) and unique per database, so two files never
  stretch the same passkey to the same key material — even with identical
  usernames and passkeys. A salt is not secret; storing it unencrypted is
  standard.
- **Crypto-agility**: the stretching parameters (algorithm, iterations, output
  length, salt) are recorded in an unencrypted `header` entry of the `.pku`
  file. A database is always reopened — and rewritten — with the **exact
  parameters stored in that header**. There is no automatic upgrade to
  `DefaultSlowHashParameters` on save today; the sticky header is what will let
  a future release migrate work factor or algorithm without breaking existing
  files. The header is not secret: lowering iterations in an *existing* file's
  header does not weaken already encrypted data (the wrong work factor simply
  yields the wrong key). What it *can* do is offer the user a **new** vault
  written under a trivial work factor. To block that, Open and every
  `GetSlowHash` call enforce a **KDF floor** via
  `EnsureSufficientSlowHashParameters`: a known algorithm, iterations at least
  **600,000** (PBKDF2-HMAC-SHA-256) or **210,000** (PBKDF2-HMAC-SHA-512) — the
  OWASP Password Storage Cheat Sheet baselines — output length ≥ 32 bytes, and a
  Base64 salt of at least 16 bytes. Parameters below the floor raise
  `InsufficientKdfParametersException` and the file is refused. New databases
  still use the stronger default of 1,000,000 PBKDF2-HMAC-SHA-512 iterations.

### Symmetric encryption (data at rest)

- The `database` and `autosave` payloads are protected with a layered
  ("onion") scheme: **each passkey adds one authenticated AES-256-GCM layer**.
- For every layer, a fresh 32-byte key is derived with **HKDF-SHA256** from the
  passkey and a random 16-byte salt; a random 12-byte nonce and a 16-byte
  authentication tag are used. Each layer is binary
  `salt | nonce | tag | ciphertext`; **Base64 is applied once** to the finished
  onion, so intermediate layers do not inflate the next by ~4/3.
- AES-GCM is an **AEAD** cipher: any tampering with the ciphertext, nonce, or tag
  is detected and rejected on decryption.
- A final layer keyed with a fixed, public value lets the code distinguish
  "corrupted or foreign data" (`CorruptedSourceException`) from "valid data,
  wrong passkey" (`WrongPasswordException`).

### Asymmetric encryption (activity log)

- Key pairs are **RSA-4096**, exported as PEM.
- The audit/activity log uses a **hybrid** scheme: a random one-time AES key
  encrypts each record symmetrically, and that key is wrapped with
  **RSA-OAEP-SHA256**. This lets activity entries be written even when the full
  symmetric passkey set is not available (e.g. a failed login records an entry
  without anyone being logged in).

### Activity-log integrity (tamper-evidence)

Because entries must be writable **without being logged in**, writing relies on
the public key alone and therefore cannot be protected by a secret. Integrity is
instead provided by **sealing**, which makes tampering *detectable* on the next
login:

- On every save performed **while a user is logged in**, the whole current log
  is sealed: an **RSA-PSS-SHA256 signature** (made with the user's private key)
  is computed over a canonical payload of the sealed entry count, the activity
  log's public key, and the sealed entry ciphertexts. Verification only needs
  the public key.
- The number of sealed entries is anchored inside the **encrypted, AEAD-protected
  database** (`ActivitySealWatermark`). Since that store is tamper-proof, it lets
  the next login detect a **rollback/truncation** of the sealed entries, or a
  **stripped** signature.
- On login the log is verified against the private key: the stored public key
  must match the key pair in the database (defeats a **key substitution**), and
  the signature must be valid over the sealed entries (defeats **modification,
  forgery and reordering** of the sealed portion).
- If any check fails, login is **not** blocked; instead a reviewable
  `ActivityLogTampered` activity is recorded so the user is alerted.

### Storage format (`.pku`)

- A `.pku` file is a **ZIP archive** containing four entries: `header`,
  `database`, `autosave`, and `activity`.
- The `header` entry holds the sticky `KdfParameters` (algorithm, iterations,
  output length, salt). It is not passkey-encrypted — only the shared
  JSON → GZip → Base64 pipeline applies — because those values must be readable
  before any key can be derived.
- Each entry pipeline is: JSON serialize → GZip compress → Base64, then (for
  `database`/`autosave`) the symmetric onion encrypts that compressed payload.
  The `activity` entry skips the onion: its records are already protected
  individually with per-record RSA hybrid encryption before that shared
  compress/Base64 step. Compressing **before** encryption is deliberate: GZip only
  shrinks structured plaintext, not high-entropy ciphertext.
- File access is serialized through a re-entrant lock (`FileLocker`) to prevent
  concurrent access races (e.g. a save colliding with the session-timeout timer).
- **Atomic ZIP commits**: each entry update builds a complete replacement archive
  in memory, writes it to a sibling temp file (flushed with write-through), then
  `File.Move(overwrite)` swaps it onto the `.pku` path. Readers therefore see
  either the previous intact archive or the new one — never a torn
  `ZipArchiveMode.Update` rewrite, and never trailing garbage when the archive
  shrinks. The session handle is released only for that replace and reacquired
  immediately afterwards.
- **Deferred persistence**: while a user is logged in, autosave and activity-log
  ZIP rewrites are coalesced with a short debounce (~500 ms) so a burst of field
  edits becomes a single disk write. Pending work is flushed on explicit `Save`
  and on `Close`. Pre-login events (open, failed login) still write immediately
  so the audit trail survives a crash before the session starts.
  The `.pku` handle is held open for the whole session (`FileShare.Read`) outside
  the brief atomic-replace window above; other processes may still open the file
  for reading, but not for writing.

### Randomness

- All security-relevant randomness uses the cryptographically secure
  `System.Security.Cryptography.RandomNumberGenerator` (keys, salts, nonces, and
  generated passwords via `RandomNumberGenerator.GetInt32`). `System.Random` is
  never used for secrets.

### In-memory hygiene

- `IDatabase.Login` takes a plain `string` passkey (there is no `SecureString`
  overload on the Core API). The WPF GUI keeps the typed secret in
  `PasswordBox.SecurePassword` and bridges it through
  `SecureStringExtensions.UseAsString`: the unmanaged BSTR is zeroed in a
  `finally` block (`Marshal.ZeroFreeBSTR`) so it only lives for the duration of
  the `Login` call. The short-lived managed `string` passed to Core remains
  subject to the usual .NET GC limitations documented under "Known Limitations".
- Derived AES keys, per-layer UTF-8 password bytes, and GCM plaintext buffers
  are wiped with `CryptographicOperations.ZeroMemory` after use (same pattern
  as `ProtectedSecret`).

### Progressive login without rollback (online brute-force friction)

- Logging in is **progressive**: each call to `IDatabase.Login` appends one
  stretched passkey to the in-memory onion stack and attempts decryption. There
  is **no rollback** of a wrong attempt. A mistyped passkey permanently
  poisons the current open session: every subsequent `Login` call keeps
  stacking on top of the bad layer, so even the correct remaining passkeys
  cannot recover the database until the session is closed and the file is
  reopened from scratch.
- That behaviour is intentional. Combined with the expensive PBKDF2 stretch on
  every attempt, it turns an interactive guessing loop into a high-friction
  path: each wrong guess both costs a full slow-hash and forces a full reopen
  (and a fresh stretch of every passkey entered so far) before the attacker can
  try again. It is a deliberate online anti-brute-force layer on top of the
  offline hardness of the onion encryption — not an accidental UX bug.
- The legitimate user who mistypes must close the database (or cancel the
  login UI, which ends the session) and start over. See also the matching note
  under "Known Limitations".

### Session protection

- **Auto-logout**: after a configurable inactivity timeout
  (`ISettings.LogoutTimeout`), the session is closed automatically and the
  database file handle is released.
- **Clipboard cleaning**: copied passwords are removed from the clipboard (and
  clipboard history, via the OS-specific `IClipboardManager`) after a
  configurable delay (`ISettings.CleaningClipboardTimeout`).

### Password hygiene features

- **Strong password generation** uses the CSPRNG over a configurable alphabet.
  When leak-checking is enabled, generation retries at most **five** candidates
  against the HIBP corpus and then gives up (returns empty) rather than hammering
  the remote service.
- **Leak detection** uses the "Have I Been Pwned" range API
  (`api.pwnedpasswords.com`) with **k-anonymity**: only the first 5 characters of
  the SHA-1 hash are sent, never the password. This is the **only** outbound
  network call the application makes, it is opt-in per account, and it **fails
  open** (treats the password as "not leaked" if the service is unreachable) so a
  network problem never blocks the user. Successful range responses are **cached
  in process** (keyed by the 5-character prefix, bounded, never persisted) so
  repeated checks for the same prefix — generation retries, duplicate accounts,
  re-scans — avoid extra round-trips. Requests time out after a few seconds.
  The GUI and the warning scan use the asynchronous API so the UI thread is not
  blocked while waiting on the network.
- **Duplicate-password** and **password-expiry** warnings are computed locally.

## Known Limitations

These are conscious trade-offs, documented for transparency:

- **Secrets in managed memory**: once decrypted, passwords are handled as .NET
  `string` values, which are immutable and cannot be reliably zeroed before
  garbage collection. An attacker able to read process memory or the OS swap file
  while the database is unlocked may recover secrets. This is consistent with the
  "compromised host" out-of-scope item.
- **Password stretching algorithm**: the project uses PBKDF2 rather than a
  memory-hard KDF such as Argon2id, because Argon2 is not part of the .NET base
  class library and the project maintains a zero-external-dependency policy for
  its core. To compensate, it uses PBKDF2-HMAC-SHA-512 (more hostile to
  GPU/ASIC parallelism than SHA-256) with 1,000,000 iterations. The sticky KDF
  header (see "Crypto-agility") keeps the door open to adopting a memory-hard
  KDF later, pluggably, should the policy ever be relaxed.
- **Import/Export files**: CSV and JSON files produced by the Export feature (and
  consumed by Import) are **unencrypted plaintext** by design, for
  interoperability. The `.csv` path is tab-separated (TSV) with JSON-encoded
  cells and covers services/accounts only; `.json` also carries user settings.
  Users are responsible for protecting or deleting these files.
- **Leak check fails open**: if the Have I Been Pwned service is unreachable, a
  potentially leaked password is reported as "not leaked".
- **Unsealed activity-log tail**: the activity log is tamper-evident only for the
  portion sealed at the last login (see "Activity-log integrity"). Entries added
  since then — including events written while no one is logged in, such as failed
  logins — are **not** protected against deletion or alteration by an attacker
  with write access to the file, because writing them requires no secret. Such an
  attacker could erase the record of their own access before the legitimate user
  logs in again. Detecting this fully would require a trusted external log; it is
  out of scope for a purely local, offline tool. Everything sealed at the last
  login, however, remains tamper-evident, and a wholesale rollback of the sealed
  portion is detected via the watermark stored in the encrypted database.
- **No login-attempt rollback**: a wrong passkey cannot be undone without closing
  and reopening the database (see "Progressive login without rollback"). This
  raises the cost of interactive guessing at the expense of UX: a legitimate
  mistype also requires a full restart of the login sequence.
- **Compressed size leakage**: `database`/`autosave` are GZip-compressed before
  encryption, so the ciphertext length tracks the compressed plaintext size.
  An observer of the `.pku` file can therefore infer approximate vault size and,
  in edge cases, rough compressibility of the JSON. Absolute lengths are already
  visible for any AEAD ciphertext; this trade-off is accepted for the storage
  savings on structured data.

## Reporting Non-Security Bugs

For regular, non-security bugs and feature requests, please use the normal public
[GitHub issues](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/issues).
