# FAQ

## Why does a typo force me to restart login?

Each `Login` call appends a stretched passkey to the in-memory onion. A wrong value is never undone, so later correct keys still fail until you `Close` and `Open` again. That is deliberate online brute-force friction on top of PBKDF2, not a bug. The WPF client uses Escape to drop the half-open session. See [[Security]] and [[Usage Cookbook]].

## Why is `Create` already logged in? Why can't I call `Login` after it?

`Create` builds a complete onion, writes the `.pku`, and returns a session whose `User` is already set. Extra `Login` calls would wrap **another** layer on a finished stack and fail. Use `Login` only after `Open`.

## Can I sync the vault with Dropbox / OneDrive / git?

The app does not sync. You can copy the `.pku` as a backup. Two machines must not write it at the same time: during a session the handle is held with `FileShare.Read`. Conflict copies (`file (1).pku`) are not merged. This is not a multi-master database.

## Is the CSV export really CSV?

The extension is `.csv`. The content is **tab-separated (TSV)** and each cell is a **JSON string**. Settings are not included. See [[Import Export]].

## Does leak checking send my password to the internet?

No. Only a hash prefix (k-anonymity): first 5 characters of SHA-1 to Have I Been Pwned, or first 10 characters of Keccak-512 to XposedOrNot if HIBP is unreachable. The password never leaves the device. The feature is opt-in per account (`WarnIfPasswordLeaked`). These are the only outbound network calls in the product.

## The leak check said nothing. Is my password safe?

If both providers are down (or you are offline), the check **fails open** and reports "not leaked". Failures are not cached, so a later successful check can still raise a warning. There is no separate "unverified" UI state. See [[Security]].

## Why PBKDF2 instead of Argon2?

Argon2 is not part of the .NET base class library. Core and Interfaces have a **zero NuGet** policy. Compensation: PBKDF2-HMAC-SHA-512 with 1,000,000 iterations, plus a sticky KDF header so a future release could adopt a memory-hard KDF without breaking old files. See [[Vault Format]].

## Can I use Core on Linux?

Yes. Build `Upsilon.Apps.Passkey.Linux.slnx` (Interfaces + Core). You must implement `IClipboardManager`. There is no official Linux GUI. Unit tests do not run on that solution (Windows TFM).

## Where is my vault file in the WPF app?

New users: `raw/{GetHash(username)}.pku` next to the executable. `GetHash` is fast SHA-512, Base64 with `/` replaced by `-`. You can also open any `.pku` with `Ctrl+O` or a command-line path. Logs: `%LocalAppData%\Passkey\logs`.

## What happens if I change my username?

The username is part of the onion's implicit first layer (`GetHash(username)`). Changing it changes the ciphertext layout on the next `Save`. It is not a secret factor by itself. Hosts that name files from the username hash (the WPF client) may also need to rename the file — Core will not do that for you.

## Import failed because the service already exists. Can I merge?

No. Import refuses the whole file if any service name collides or is blank. Rename or delete the existing service, or edit the import file. See [[Import Export]].

## Is the activity log a secure audit that cannot be tampered with?

It is **tamper-evident** for the portion sealed at the last logged-in save (signature + watermark inside the encrypted database). The unsealed tail — including failed logins recorded while nobody is logged in — can be deleted by someone with write access to the file. Login is not blocked on a failed integrity check; you get `ActivityLogTampered` to review. See [[Warnings and Activity]] and [[Threat Model]].

## How do I report a security issue?

Privately: GitHub Security Advisories or contact@yassinlokhat.com. Never a public issue. SLAs and supported versions: [[Security]].
