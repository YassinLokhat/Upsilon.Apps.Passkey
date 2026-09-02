# Upsilon.Apps.Passkey

A **local-only** password manager written in C# on **.NET 10**. There is no server, no account, and no synchronization: every secret lives in a single encrypted `.pku` file on the user's device.

<!-- BEGIN:versions-summary -->At the time of writing, `Upsilon.Apps.Passkey.Interfaces` is on **1.0.x**; `Upsilon.Apps.Passkey.GUI.WPF`, `Upsilon.Apps.Passkey.Core` and `Upsilon.Apps.Passkey.Utils` are on **1.1.x**. Each assembly is versioned independently and may diverge. Always upgrade to the latest release of a component before reporting an issue.<!-- END:versions-summary -->

## What it stores

* Services (sites or apps), accounts, identifiers, notes, and dated password history
* An ordered set of **master passkeys** that form an AES-256-GCM onion (see [[Security]] and [[Vault Format]])
* A tamper-evident **activity log**
* Local **warnings**: password-update reminders, duplicates, leaks, and activity review

## What it does not do

* Cloud backup, multi-device sync, or a hosted user account
* Protect data if the host is already compromised (malware, keylogger, memory scraper) while the vault is unlocked — see [[Threat Model]]
* Encrypt the JSON/CSV files you deliberately produce via import/export — those are plaintext by design

## Features at a glance

| Area | Behaviour |
| ---- | --------- |
| At rest | AES-256-GCM onion over ordered passkeys; sticky KDF header in the ZIP |
| In memory | Account passwords, passkeys, and the RSA private key wrapped with `ProtectedSecret` |
| Session | Configurable auto-logout, clipboard auto-clear (including Windows clipboard history) |
| Login | Progressive passkeys **without rollback** (online brute-force friction) |
| Generation | CSPRNG over a configurable alphabet |
| Leak checks | Opt-in Have I Been Pwned, then XposedOrNot failover, then an optional local HIBP Bloom filter (`.pkbf`; k-anonymity / offline) |
| Import / export | Plaintext JSON (settings + services) or CSV (services only; import accepts comma- or tab-delimited) |
| Windows client | System / Light / Dark theme, QR codes, global paste hotkeys, autosave merge on next login, App Settings for vault folder and offline leak DB |

## Start here

| You want to… | Page |
| ------------ | ---- |
| Install a Windows release or build from source | [[Getting Started]] |
| Understand layers and solutions | [[Architecture]] |
| Embed Core in your own host | [[Core API]] and [[Usage Cookbook]] |
| Move data in or out | [[Import Export]] |
| How `.pku` and cryptography work | [[Vault Format]] and [[Security]] |
| GUI shortcuts, QR, clipboard | [[WPF Client]] |
| Warnings and the audit trail | [[Warnings and Activity]] |
| Tests, coverage, GitHub Actions | [[Testing and CI]] |

## Reporting problems

* **Security vulnerabilities** — GitHub Security Advisories or email. Never public issues. See [[Security]].
* **Bugs and features** — [GitHub issues](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/issues).

**License:** GNU General Public License v2.0. See [`LICENSE`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/LICENSE).

This wiki lives in the `Wiki/` folder of the source repository so it can be reviewed with the code. To publish it as the GitHub Wiki, copy the Markdown files to the `Upsilon.Apps.Passkey.wiki.git` remote (see [[Getting Started]]).
