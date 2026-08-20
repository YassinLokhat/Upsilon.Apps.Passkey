# Getting Started

## Requirements

* **.NET 10 SDK**
* **Windows** for the WPF client (`net10.0-windows10.0.18362.0`) and for tests (the test project uses a Windows TFM)
* **Linux** can build Interfaces + Core only (`Upsilon.Apps.Passkey.Linux.slnx`) — there is no official Linux GUI

## Clone and build

```bash
git clone https://github.com/YassinLokhat/Upsilon.Apps.Passkey.git
cd Upsilon.Apps.Passkey

# Windows: GUI + tests
dotnet build Upsilon.Apps.Passkey.Windows.slnx
dotnet run --project GUI/WPF

# Linux / Core-only
dotnet build Upsilon.Apps.Passkey.Linux.slnx
```

Run tests on Windows:

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
```

See [[Testing and CI]] for coverage, GUI ViewModel filters, and workflows.

## First vault in the WPF client

Concrete flow:

1. Run `dotnet run --project GUI/WPF`.
2. Create a user (`Ctrl+N`). Choose a username and **ordered passkeys**. One passkey is allowed; two or more make a real multi-factor onion.
3. The file is created next to the executable as `raw/{GetHash(username)}.pku`. `GetHash` is fast SHA-512, Base64 with `/` replaced by `-`.
4. Close the app, or wait for auto-logout.
5. Reopen with `Ctrl+O`, or pass the `.pku` path as the **first command-line argument**.
6. Type the username, then **each passkey in the same order**.

A mistyped passkey cannot be undone. Press Escape (or otherwise close the half-open session) and start login again. That is intentional — see [[Security]].

More GUI behaviour: [[WPF Client]].

## Embed Core without WPF

You must supply an `IClipboardManager` (OS-specific). Core already ships `CryptographyCenter`, `JsonSerializationCenter`, and `PasswordFactory` in `Upsilon.Apps.Passkey.Core.Utils`.

```csharp
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Models;

IDatabase database = Database.Create(
   new CryptographyCenter(),
   new JsonSerializationCenter(),
   new PasswordFactory(),
   new OsClipboardManager(), // you implement IClipboardManager
   "./alice.pku",
   "alice",
   ["correct-horse", "battery-staple"]);

IUser user = database.User!; // already logged in after Create — do not Login again
IService github = user.AddService("GitHub");
github.Url = new Uri("https://github.com");
github.AddAccount("work", ["alice@example.com"], "a-long-generated-secret");
database.Save();
database.Close();
```

`Create` / `CreateAsync` open the database **and log the user in**. Calling `Login` afterwards would append another onion layer on an already-complete stack and fail. Progressive `Login` is only needed after `Open`.

Prefer `CreateAsync` / `OpenAsync` / `LoginAsync` from a UI: creating a vault mints an RSA-4096 key pair, and stretching a single passkey costs about a second by design.

Full API notes: [[Core API]]. More scenarios: [[Usage Cookbook]].

## Publish this folder as a GitHub Wiki

GitHub Wikis are a **separate git remote**, not the `Wiki/` directory itself. After enabling Wikis on the repository:

```bash
git clone https://github.com/YassinLokhat/Upsilon.Apps.Passkey.wiki.git
# Copy Wiki/*.md (including _Sidebar.md and _Footer.md) to the clone root, then commit and push.
```

Keep `Wiki/` in the source repo so documentation changes can go through the same pull requests as code.
