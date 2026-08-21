# Contributing

Contributions are welcome. Please open a pull request against `master` with a
focused change and enough context for review.

## Before you start

- **Security issues** must not be filed as public GitHub issues. Follow
  [SECURITY.md](SECURITY.md) instead.
- Keep Core and Interfaces free of third-party NuGet packages (see below).
- Prefer a small PR over a mixed refactor + feature + docs dump.

## Repository layout

| Path | Role |
| ---- | ---- |
| `Interfaces/` | Public contracts (`IDatabase`, crypto, serialization, clipboard). |
| `Core/` | Vault implementation: onion encryption, `.pku` I/O, warnings, import/export. `Database` is a partial class; internal hosts (`IActivityHost`, `IAutoSaveHost`, `IUserHost`) keep ActivityCenter / AutoSave / User from digging into Database members. |
| `GUI/WPF/` | Windows desktop client (WPF, .NET 10 Windows TFM). |
| `UnitTests/` | Core tests plus ViewModel tests through the `AppServices` seam. |

Two solution files exist on purpose:

- `Upsilon.Apps.Passkey.Windows.slnx` — Interfaces, Core, WPF GUI, and tests.
- `Upsilon.Apps.Passkey.Linux.slnx` — Interfaces and Core only (no WPF, no tests:
  the test project targets `net10.0-windows`).

## Build and test

```bash
dotnet build Upsilon.Apps.Passkey.Windows.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
```

Windows CI also enforces **90% line coverage of `Upsilon.Apps.Passkey.Core`**.
Coverage is scoped in `coverage.runsettings`; the WPF assembly is excluded.
Do not lower that gate without an explicit discussion in the PR.

GUI ViewModel tests can be filtered with:

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Gui"
```

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global
hotkeys, and real MessageBoxes stay in the [manual smoke list](README.md#manual-smoke-gui).

## Zero-dependency policy (Core and Interfaces)

`Core` and `Interfaces` must not take a `PackageReference`. An MSBuild target
fails the build if one appears. That keeps the vault's supply-chain surface
limited to the .NET BCL.

Allowed:

- In-solution `ProjectReference`s.
- Packages in `UnitTests` (MSTest, FluentAssertions 7.x).
- GitHub Actions / CodeQL on the CI runners (not referenced by the libraries).

The WPF project currently has no NuGet packages either; keep it that way unless
a Windows-only capability cannot be done with the BCL.

## Adding a UI language

1. Copy `GUI/WPF/Localization/Strings.resx` → `Strings.xx.resx` and translate
   values (do not rename keys).
2. Register `new("xx", "Native name")` in `LocalizationService.Supported`.
3. Prefer `{loc:Loc Key}` in XAML and `Strings.Key` / `Strings.Format` in C#.
   See `Wiki/WPF-Client.md`.

## Code style

Shared rules live in [`.editorconfig`](.editorconfig) and
[`Directory.Build.props`](Directory.Build.props):

- Nullable enabled, implicit usings, latest analyzers, **warnings as errors**.
- Indent: 3 spaces, CRLF, UTF-8 with BOM for C#.
- Private fields and private methods: `_camelCase`.
- Private `const` fields: `SCREAMING_SNAKE`.
- Interfaces: `I…` prefix.

Match the surrounding file. Do not reformat unrelated code.

## What a PR should include

- Tests for Core behaviour you change (crypto, vault lifecycle, import/export,
  warnings, persistence).
- ViewModel tests when you change GUI logic that already sits behind
  `AppServices` (dialogs, session, clipboard, navigation).
- README / SECURITY.md updates when you change a public contract, a threat-model
  assumption, or a user-visible security behaviour.
- No secrets: vault files, exported JSON/CSV, logs, or credentials.

## Commit messages

Write a short subject that states **why** the change exists (fix, add, update),
not a file list. Follow the recent history on `master`.
