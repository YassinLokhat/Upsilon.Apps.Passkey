# Vault Format

A `.pku` file is a **ZIP archive** with four entries. All cryptography is in `Core/Utils/CryptographyCenter.cs` on top of `System.Security.Cryptography`.

## ZIP entries

| Entry | Passkey onion? | Contents |
| ----- | -------------- | -------- |
| `header` | No | Sticky `KdfParameters` (algorithm, iterations, output length, salt) |
| `database` | Yes | User, settings, services, accounts, activity-log seal watermark |
| `autosave` | Yes | Unsaved edits until `Save()` |
| `activity` | No onion; per-record RSA hybrid | Audit trail |

The `header` is not passkey-encrypted — only the shared JSON → GZip → Base64 pipeline applies — because those values must be readable **before** any key can be derived. A salt is not secret; storing it unencrypted is standard.

## Serialization pipeline

For `database` / `autosave`:

1. JSON serialize
2. GZip compress
3. Base64
4. Symmetric onion encrypts that compressed payload

The `activity` entry skips the onion: records are already protected individually with per-record RSA hybrid encryption before the shared compress/Base64 step.

Compressing **before** encryption is deliberate: GZip shrinks structured plaintext, not high-entropy ciphertext. The trade-off is compressed size leakage (ciphertext length tracks approximate vault size) — documented under known limitations on [[Security]].

## Master passkeys (onion)

1. The onion always starts with an **implicit first layer keyed by the username**: `GetHash(username)` (fast SHA-512, Base64 with `/` replaced by `-`) is pushed onto the stack before any stretched passkey. Changing the username changes the ciphertext layout. It is not a secret factor on its own, but it is part of the key material used at rest.
2. Each passkey is stretched with **PBKDF2-HMAC-SHA-512, 1,000,000 iterations** (64-byte output) before use (`GetSlowHash`). HMAC-SHA-512's 64-bit arithmetic is less efficient on the GPUs/ASICs an attacker would use for parallel guessing than SHA-256.
3. The PBKDF2 salt is a **random 128-bit value generated once at Create** and stored in `header`. It is stable for the life of the file and unique per database, so two files never stretch the same passkey to the same key material — even with identical usernames and passkeys.
4. Each stretched passkey adds one **authenticated AES-256-GCM** layer. For every layer, a fresh 32-byte key is derived with **HKDF-SHA256** from the passkey and a random 16-byte salt; a random 12-byte nonce and a 16-byte tag are used. Binary layout per layer: `salt | nonce | tag | ciphertext`. **Base64 is applied once** to the finished onion so intermediate layers do not inflate the next by ~4/3.
5. A final layer keyed with a **fixed, public value** lets the code distinguish "corrupted or foreign data" (`CorruptedSourceException`) from "valid data, wrong passkey" (`WrongPasswordException`).

AES-GCM is AEAD: tampering with ciphertext, nonce, or tag is detected and rejected.

### Crypto-agility and the KDF floor

Stretching parameters are recorded in `header`. A database is always reopened — and rewritten — with the **exact parameters stored there**. There is no automatic upgrade to `DefaultSlowHashParameters` on save today.

Open and every `GetSlowHash` call enforce a **KDF floor** via `EnsureSufficientSlowHashParameters`:

* A known algorithm (`Pbkdf2HmacSha256` or `Pbkdf2HmacSha512`)
* Iterations at least **600,000** (SHA-256) or **210,000** (SHA-512) — OWASP Password Storage Cheat Sheet baselines
* Output length ≥ 32 bytes
* A Base64 salt of at least 16 bytes

Parameters below the floor raise `InsufficientKdfParametersException` and the file is refused. New databases still use the stronger default of 1,000,000 PBKDF2-HMAC-SHA-512 iterations.

Lowering iterations in an *existing* file's header does not weaken already encrypted data (the wrong work factor simply yields the wrong key). What it *can* do is offer the user a **new** vault written under a trivial work factor — that is what the floor blocks.

## Activity log encryption

Key pairs are **RSA-4096**, exported as PEM. The audit log uses a **hybrid** scheme: a random one-time AES key encrypts each record symmetrically, and that key is wrapped with **RSA-OAEP-SHA256**. Entries can be written even when the full symmetric passkey set is not available (for example a failed login). Integrity of that log is a separate story — see [[Warnings and Activity]] and [[Security]].

## Atomic writes and locking

* File access is serialized through a re-entrant lock (`FileLocker`) so a save cannot collide with the session-timeout timer.
* Each entry update builds a complete replacement archive in memory, writes it to a sibling temp file (flushed with write-through), then `File.Move(overwrite)` swaps it onto the `.pku` path. Readers see either the previous intact archive or the new one — never a torn `ZipArchiveMode.Update` rewrite, and never trailing garbage when the archive shrinks.
* The session handle is released only for that replace and reacquired immediately afterwards. Outside that window the handle stays open with `FileShare.Read`: other processes may read the file, but not write it.

### Deferred persistence

While a user is logged in, autosave and activity-log ZIP rewrites are coalesced with a short debounce (~500 ms) so a burst of field edits becomes a single disk write. Pending work is flushed on explicit `Save` and on `Close`. Pre-login events still write immediately.

## Concrete scenario: crash during edits

1. The user changes an account password. That writes (debounced) into the `autosave` ZIP entry, not yet into `database`.
2. The process is killed before `Save()`.
3. The next successful login raises `AutoSaveDetected`. The host sets `AutoSaveMergeBehavior` on the event args:

| Value | Meaning |
| ----- | ------- |
| `MergeAndSaveThenRemoveAutoSaveFile` | Take autosave, persist as `database`, drop the autosave entry |
| `MergeWithoutSavingAndKeepAutoSaveFile` | Load autosave in memory, keep the ZIP entry |
| `DontMergeAndRemoveAutoSaveFile` | Discard unsaved work and drop autosave |
| `DontMergeAndKeepAutoSaveFile` | Ignore for now, keep the entry |

`Undefined` is the default before the handler chooses. The GUI should always set an explicit behaviour.

Related API: [[Core API]], [[Usage Cookbook]].
