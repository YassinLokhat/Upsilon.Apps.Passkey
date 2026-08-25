# Contributing

Contributions are welcome. Open a pull request against `master` with a focused change and enough context for review.

This page mirrors [`CONTRIBUTING.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/CONTRIBUTING.md). Layout details: [[Architecture]]. Tests: [[Testing and CI]]. Security reports: [[Security]] — **not** public issues.

## Before you start

* Keep Core, Utils, and Interfaces free of third-party NuGet packages.
* Prefer a small PR over a mixed refactor + feature + docs dump.
* When changing Core internals, respect the host surfaces (`IActivityHost`, `IAutoSaveHost`, `IUserHost`) documented in [[Architecture]] — do not reintroduce reverse dependencies from ActivityCenter / AutoSave / User into `Database` members.

## Build and test

```bash
dotnet build Upsilon.Apps.Passkey.Windows.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
```

GUI ViewModel tests:

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Gui"
```

## Zero-dependency policy (Core, Utils, and Interfaces)

`Core`, `Utils`, and `Interfaces` must not take a `PackageReference`. An MSBuild target fails the build if one appears. That keeps the vault's supply-chain surface limited to the .NET BCL.

Allowed:

* In-solution `ProjectReference`s
* Packages in `UnitTests` (MSTest, FluentAssertions 7.x)
* GitHub Actions / CodeQL on the CI runners (not referenced by the libraries)

The WPF project currently has no NuGet packages either; keep it that way unless a Windows-only capability cannot be done with the BCL.

## Adding a UI language

1. Copy `GUI/WPF/Localization/Strings.resx` → `Strings.xx.resx` and translate values (do not rename keys).
2. Register `new("xx", "Native name")` in `LocalizationService.Supported`.
3. Prefer `{loc:Loc Key}` in XAML and `Strings.Key` / `Strings.Format` in C#. Details: [[WPF Client]].

## Code style

Shared rules live in `.editorconfig` and `Directory.Build.props`:

* Nullable enabled, implicit usings, latest analyzers, **warnings as errors**
* Indent: **3 spaces**, CRLF, UTF-8 **with BOM** for C#
* Private fields and private methods: `_camelCase`
* Private `const` fields: `SCREAMING_SNAKE`
* Interfaces: `I…` prefix

Match the surrounding file. Do not reformat unrelated code.

## What a PR should include

* Tests for Core/Utils behaviour you change
* ViewModel tests when you change GUI logic behind `AppServices`
* README / SECURITY.md / wiki updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour
* No secrets: vault files, exported JSON/CSV, logs, or credentials

## Commit messages

Write a short subject that states **why** the change exists (fix, add, update), not a file list. Follow the recent history on `master`.

## Wiki edits

Keep pages in the source tree under `Wiki/` so they review with the code. To publish to GitHub's Wiki hosting, copy the Markdown files to `Upsilon.Apps.Passkey.wiki.git` (see [[Getting Started]]). Use `[[Page Title]]` links so both the folder and the GitHub Wiki resolve them.
