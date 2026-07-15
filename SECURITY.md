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
- Each passkey is stretched with **PBKDF2-HMAC-SHA256, 1,000,000 iterations**
  (64-byte output) before being used as key material (`GetSlowHash`). This far
  exceeds the OWASP baseline for PBKDF2-SHA256 and makes offline guessing
  expensive.
- The PBKDF2 salt is derived deterministically from the username
  (`SHA-256(fixed_prefix || username)`), so the same username always yields the
  same salt (required to reopen the file) while different usernames get distinct
  salts.

### Symmetric encryption (data at rest)

- The `database` and `autosave` payloads are protected with a layered
  ("onion") scheme: **each passkey adds one authenticated AES-256-GCM layer**.
- For every layer, a fresh 32-byte key is derived with **HKDF-SHA256** from the
  passkey and a random 16-byte salt; a random 12-byte nonce and a 16-byte
  authentication tag are used. The stored layout is
  `salt | nonce | tag | ciphertext` (Base64), so decryption is self-describing.
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
  symmetric passkey set is not available.

### Storage format (`.pku`)

- A `.pku` file is a **ZIP archive** containing three entries: `database`,
  `autosave`, and `activity`.
- Each entry pipeline is: JSON serialize → encrypt (symmetric onion for
  `database`/`autosave`, per-record RSA for `activity`) → GZip compress →
  Base64 → write into the ZIP entry.
- File access is serialized through a re-entrant lock (`FileLocker`) to prevent
  concurrent access races (e.g. a save colliding with the session-timeout timer).

### Randomness

- All security-relevant randomness uses the cryptographically secure
  `System.Security.Cryptography.RandomNumberGenerator` (keys, salts, nonces, and
  generated passwords via `RandomNumberGenerator.GetInt32`). `System.Random` is
  never used for secrets.

### In-memory hygiene

- The `SecureString` login path copies the secret into a transient buffer that is
  zeroed in a `finally` block, and frees the unmanaged BSTR
  (`Marshal.ZeroFreeBSTR`).
- Derived AES keys are wiped with `CryptographicOperations.ZeroMemory` after use.

### Session protection

- **Auto-logout**: after a configurable inactivity timeout (`LogoutTimeout`), the
  session is closed automatically and the database file handle is released.
- **Clipboard cleaning**: copied passwords are removed from the clipboard (and
  clipboard history, via the OS-specific `IClipboardManager`) after a
  configurable delay (`CleaningClipboardTimeout`).

### Password hygiene features

- **Strong password generation** uses the CSPRNG over a configurable alphabet.
- **Leak detection** uses the "Have I Been Pwned" range API
  (`api.pwnedpasswords.com`) with **k-anonymity**: only the first 5 characters of
  the SHA-1 hash are sent, never the password. This is the **only** outbound
  network call the application makes, it is opt-in per account, and it **fails
  open** (treats the password as "not leaked" if the service is unreachable) so a
  network problem never blocks the user.
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
  its core. PBKDF2 with 1,000,000 SHA-256 iterations is used to compensate.
- **Deterministic salt**: the PBKDF2 salt is derived from the username, so two
  users with the same username share the same salt. Usernames are not secrets.
- **Import/Export files**: CSV and JSON files produced by the Export feature (and
  consumed by Import) are **unencrypted plaintext** by design, for
  interoperability. Users are responsible for protecting or deleting them.
- **Leak check fails open**: if the Have I Been Pwned service is unreachable, a
  potentially leaked password is reported as "not leaked".

## Reporting Non-Security Bugs

For regular, non-security bugs and feature requests, please use the normal public
[GitHub issues](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/issues).
