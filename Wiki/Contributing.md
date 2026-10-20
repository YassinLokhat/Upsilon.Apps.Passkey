# Contributing

Contributions are welcome. Open a pull request against `master` with a focused change and enough context for review.

This page mirrors [`CONTRIBUTING.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/CONTRIBUTING.md). Layout details: [[Architecture]]. Tests: [[Testing and CI]]. Security reports: [[Security]] — **not** public issues.

## Before you start

* Keep Core, Utils, Interfaces, and the WPF GUI free of third-party NuGet packages.
* Prefer a small PR over a mixed refactor + feature + docs dump.
* When changing Core internals, respect the host surfaces (`IActivityHost`, `IAutoSaveHost`, `IUserHost`) documented in [[Architecture]] — do not reintroduce reverse dependencies from ActivityCenter / AutoSave / User into `Database` members.

## Build and test

```bash
dotnet build Upsilon.Apps.Passkey.Windows.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Linux.slnx
```

Shared ViewModel tests on their own:

```bash
dotnet test UnitTests/Presentation/UnitTests.Presentation.csproj
```

## Zero-dependency policy (Core, Utils, Interfaces, and WPF)

`Core`, `Utils`, `Interfaces`, and the WPF GUI must not take a third-party `PackageReference`. Projects opt in with `<EnforceZeroExternalDependencies>true</EnforceZeroExternalDependencies>`; `Directory.Build.targets` fails the build if a disallowed package appears. That keeps the vault's supply-chain surface limited to the .NET BCL.

Allowed:

* In-solution `ProjectReference`s
* Packages in `UnitTests` (MSTest, FluentAssertions 7.x)
* GitHub Actions / CodeQL on the CI runners (not referenced by the libraries)
* SDK toolchain packages injected by the .NET SDK during publish (today: `Microsoft.NET.ILLink.Tasks` for `PublishSingleFile` / trim / AOT), allowlisted in `Directory.Build.targets`

## Adding a UI language

1. Copy `GUI/Presentation/Localization/Strings.resx` → `Strings.xx.resx` and translate values (do not rename keys). The resources are shared by the WPF and MAUI clients.
2. Register `new("xx", "Native name")` in `LocalizationService.Shipped` (keep `System` as the follow-OS preference at the top of `Supported`), and add `xx` to `SatelliteResourceLanguages` in the MAUI `.csproj`.
3. Run `dotnet test … --filter "FullyQualifiedName~LocalizationTests"` (or the full GUI filter). Tests verify satellite key parity and localized enum/activity strings.
4. Prefer `{loc:Loc Key}` in XAML and `Strings.Key` / `Strings.Format` in C#.
5. Every new dialog `Window` must implement `ILanguageAware` and forward to its DataContext in `OnLanguageChanged` (see **Live-refresh contract** in [[WPF Client]]). Titles and combo snapshots will not update otherwise.

Key prefixes, and why each `ActivityEventType` has both `EnumValue_ActivityEventType_*` (short filter label) and `Activity_*` (full Message sentence), are documented under **Localization** in [[WPF Client]].

## Code style

Shared rules live in `.editorconfig` and `Directory.Build.props`:

* Nullable enabled, implicit usings, latest analyzers, **warnings as errors**
* Indent: **3 spaces**, CRLF, UTF-8 **with BOM** for C#
* Private fields and private methods: `_camelCase`
* Private `const` fields: `SCREAMING_SNAKE`
* Interfaces: `I…` prefix
* ViewModels live in `GUI/Presentation`: inherit its `ObservableObject`; use `RelayCommand` / `AsyncRelayCommand`; ask the user via the async `IDialogService`; open views via `INavigationService`. Hosts keep only view glue (WPF windows through `IWindowDialogService`, MAUI pages)
* Helpers: `ItemExtensions` (Interfaces), `SerializationHelpers` (Core)

Match the surrounding file. Do not reformat unrelated code.

## What a PR should include

* Tests for Core/Utils behaviour you change (crypto, vault lifecycle, import/export, alerts, persistence)
* ViewModel tests in `UnitTests/Presentation` when you change shared GUI logic behind `AppServices`
* User-visible features in both clients: the logic in `GUI/Presentation`, a view in WPF and in MAUI; a gap that remains goes in [[MAUI Parity Checklist]]
* README / SECURITY.md / wiki updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour
* No secrets: vault files, exported JSON/CSV, logs, or credentials

## Cutting a release

Package versions and dependency ranges live in [`versions.json`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/versions.json). Edit that file, run `.\scripts\Sync-Versions.ps1 -SyncOnly`, commit, merge to `master`, wait for CI, then push **per-component** tags:

```bash
git checkout master
git pull
git tag core-v2.1.0
git tag wpf-v2.1.0
git tag maui-v2.0.0
git push origin core-v2.1.0 wpf-v2.1.0 maui-v2.0.0
```

Each tag triggers `release.yml`, which publishes the GitHub Release. Do not tag a package that did not change, and run the manual smoke list in [[MAUI Parity Checklist]] before a `maui-v*` tag. Local publish of every shippable package: `.\scripts\Sync-Versions.ps1` (writes `_artifacts/`; add `-SkipAndroid` without a release keystore). Details: [[Testing and CI]].

## Commit messages

Write a short subject that states **why** the change exists (fix, add, update), not a file list. Follow the recent history on `master`.

## Wiki edits

Keep pages in the source tree under `Wiki/` so they review with the code. Merges to `master` that change `Wiki/` are published automatically to the GitHub Wiki (see [[Getting Started]]). Use `[[Page Title]]` links so both the folder and the GitHub Wiki resolve them.
