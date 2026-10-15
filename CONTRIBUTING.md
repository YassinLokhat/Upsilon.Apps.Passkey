# Contributing

Contributions are welcome. Please open a pull request against `master` with a
focused change and enough context for review.

## Before you start

- **Security issues** must not be filed as public GitHub issues. Follow
  [SECURITY.md](SECURITY.md) instead.
- Keep Core, Utils, Interfaces, Presentation, and the WPF GUI free of third-party NuGet packages; the MAUI host may reference only `Microsoft.Maui.*` (see below).
- Prefer a small PR over a mixed refactor + feature + docs dump.

## Repository layout

| Path | Role |
| ---- | ---- |
| `Interfaces/` | Public contracts (`IDatabase`, crypto, serialization, clipboard, `IProtectedSecret`, `ISecretMemoryProtector`). |
| `Utils/` | Default implementations: `CryptographyCenter`, `JsonSerializationCenter`, `PasswordFactory`, `SecretMemoryProtector` / `ProtectedSecret`, `LeakFilter/` (`.pkbf` Bloom). Same zero-NuGet policy as Core. |
| `Core/` | Vault implementation (Interfaces only — no ProjectReference to Utils): onion encryption, `.pku` I/O, alerts, import/export. `Database` is a partial class; internal hosts (`IActivityHost`, `IAutoSaveHost`, `IUserHost`) keep ActivityCenter / AutoSave / User from digging into Database members. |
| `GUI/Presentation/` | Shared ViewModels/ports (`net10.0`, zero NuGet) for WPF and MAUI. |
| `GUI/WPF/` | Windows desktop client (WPF, .NET 10 Windows TFM). |
| `GUI/MAUI/` | MAUI host (`net10.0-android` + `net10.0-windows10.0.19041.0`); Microsoft.Maui allowlist only. |
| `UnitTests/Multiplateform/` | Core/Utils tests (`net10.0`; Linux + Windows CI). |
| `UnitTests/Presentation/` | Shared Presentation ports, services and ViewModels (`net10.0`; Linux, Windows and MAUI Windows CI). |
| `UnitTests/Windows/` | WPF host tests: theme, `{loc:Loc}` extension, language delivery to open windows (`net10.0-windows`). |

Three solution files exist on purpose:

- `Upsilon.Apps.Passkey.Windows.slnx` — Interfaces, Utils, Core, Presentation, WPF GUI, and Windows/Presentation/Multiplateform tests.
- `Upsilon.Apps.Passkey.Linux.slnx` — Interfaces, Utils, Core, Presentation, Multiplateform + Presentation tests (no WPF/MAUI host).
- `Upsilon.Apps.Passkey.MAUI.slnx` — Interfaces, Utils, Core, Presentation, MAUI host, Presentation tests.

## Build and test

```bash
dotnet build Upsilon.Apps.Passkey.Windows.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Linux.slnx
```

Windows and Linux CI enforce **90% line coverage of `Upsilon.Apps.Passkey.Core`**.
Coverage is scoped in `coverage.runsettings` to the Core assembly; Utils and the
WPF assembly are excluded. Do not lower that gate without an explicit discussion
in the PR. Coverage reports from `run_code_coverage.bat` / Windows CI land in
`_testResult/` (gitignored).

Shared ViewModel tests run on their own with:

```bash
dotnet test UnitTests/Presentation/UnitTests.Presentation.csproj
```

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, keyboard
shortcuts, and themed confirmation dialogs (`ThemedMessageBoxView`) stay in the [manual smoke list](README.md#manual-smoke-gui).

## Zero-dependency policy (Core, Utils, Interfaces, Presentation, and WPF)

`Core`, `Utils`, `Interfaces`, `GUI/Presentation`, and the WPF GUI must not take
a third-party `PackageReference`. Projects opt in with
`<EnforceZeroExternalDependencies>true</EnforceZeroExternalDependencies>`;
`Directory.Build.targets` fails the build if a disallowed package appears.
That keeps the vault's supply-chain surface limited to the .NET BCL.

**MAUI host exception:** `GUI/MAUI` may set
`<AllowedExternalPackagePrefixes>Microsoft.Maui.</AllowedExternalPackagePrefixes>`
so only `Microsoft.Maui.*` direct packages (today: `Microsoft.Maui.Controls`)
pass the gate. Transitive Windows App SDK / AndroidX packages are locked via
`packages.lock.json` + `--locked-mode`, audited for vulnerabilities, and
constrained by root `nuget.config` (nuget.org only, signature validation).
See [[ADR-001-MAUI-Host-and-Multi-Device]].

Allowed:

- In-solution `ProjectReference`s.
- Packages in the test projects (MSTest, FluentAssertions 7.x).
- GitHub Actions / CodeQL on the CI runners (not referenced by the libraries).
- SDK toolchain packages injected by the .NET SDK during publish (today:
  `Microsoft.NET.ILLink.Tasks` for `PublishSingleFile` / trim / AOT). These are
  not app dependencies and are allowlisted in `Directory.Build.targets`.
- On the MAUI host only: `Microsoft.Maui.*` per `AllowedExternalPackagePrefixes`.

Workflow `uses:` entries are **pinned to full commit SHAs** (with a `# vX.Y.Z`
comment). Dependabot is enabled for the `github-actions` ecosystem only
(`.github/dependabot.yml`) so those pins can be refreshed without opening NuGet
PRs against Core/Utils/Interfaces.

Windows and Linux CI run `dotnet list <sln> package --vulnerable --include-transitive`
after restore and fail if vulnerable packages are reported (test projects are the
usual surface).

## Adding a UI language

1. Copy `GUI/Presentation/Localization/Strings.resx` → `Strings.xx.resx` and translate
   values (do not rename keys). The resources are shared by the WPF and MAUI clients.
2. Register `new("xx", "Native name")` in `LocalizationService.Shipped`
   (keep `System` as the follow-OS preference at the top of `Supported`), and add
   `xx` to `SatelliteResourceLanguages` in the MAUI `.csproj`.
3. Run `dotnet test … --filter "FullyQualifiedName~LocalizationTests"` (or the full
   GUI filter). Tests verify satellite key parity and localized enum/activity strings.
4. Prefer `{loc:Loc Key}` in XAML and `Strings.Key` / `Strings.Format` in C#.
5. Every new dialog `Window` must implement `ILanguageAware` and forward to its
   DataContext in `OnLanguageChanged` (see **Live-refresh contract** in
   `Wiki/WPF-Client.md`). Titles and combo snapshots will not update otherwise.

Key prefixes, and why each `ActivityEventType` has both
`EnumValue_ActivityEventType_*` (short filter label) and `Activity_*` (full
Message sentence), are documented under **Localization** in
`Wiki/WPF-Client.md`.

## Code style

Shared rules live in [`.editorconfig`](.editorconfig) and
[`Directory.Build.props`](Directory.Build.props):

- Nullable enabled, implicit usings, latest analyzers, **warnings as errors**.
- Indent: 3 spaces, CRLF, UTF-8 with BOM for C#.
- Private fields and private methods: `_camelCase`.
- Private `const` fields: `SCREAMING_SNAKE`.
- Interfaces: `I…` prefix.
- ViewModels live in `GUI/Presentation`: inherit its `ObservableObject`; use `RelayCommand` / `AsyncRelayCommand`; ask the user via the async `IDialogService`; open views via `INavigationService`. Hosts keep only view glue (WPF windows through `IWindowDialogService`, MAUI pages).
- Helpers: `ItemExtensions` (Interfaces), `SerializationHelpers` (Core) — do not reintroduce a `StaticMethods` type name.

Match the surrounding file. Do not reformat unrelated code.

## What a PR should include

- Tests for Core/Utils behaviour you change (crypto, vault lifecycle, import/export,
  alerts, persistence).
- ViewModel tests in `UnitTests/Presentation` when you change shared GUI logic
  (it sits behind `AppServices`: dialogs, session, clipboard, navigation);
  `UnitTests/Windows` is only for WPF host code.
- User-visible features in both clients: the logic in `GUI/Presentation`, a view
  in WPF and in MAUI. A gap that remains (platform limit or follow-up) goes in
  [Wiki/MAUI-Parity-Checklist.md](Wiki/MAUI-Parity-Checklist.md).
- README / SECURITY.md updates when you change a public contract, a threat-model
  assumption, or a user-visible security behaviour.
- No secrets: vault files, exported JSON/CSV, logs, or credentials.

## Cutting a release

Package versions and dependency ranges live in [`versions.json`](versions.json).
Edit that file, then sync the tree and (optionally) publish locally:

```powershell
# After editing versions.json
.\scripts\Sync-Versions.ps1 -SyncOnly

# Sync + publish every shippable package into .\_artifacts (replaces publish.bat)
.\scripts\Sync-Versions.ps1
```

Commit the synced `.csproj` / docs, merge to `master`, wait for CI, then push a
**per-component** tag for each package you intend to ship:

```bash
git checkout master
git pull
git tag utils-v1.1.0
git tag core-v1.1.0
git tag wpf-v1.1.0
git push origin utils-v1.1.0 core-v1.1.0 wpf-v1.1.0
```

Do **not** bump or tag a package whose public surface did not change (for example
leave `interfaces` at `1.0.0` if Interfaces is unchanged).

There is currently **no** automated `release.yml` workflow in this repository.
Publish GitHub Releases manually after tagging (attach `.nupkg` / `.sha256` for
libraries and `Upsilon.Apps.Passkey.GUI.WPF-{version}-win-x64.zip` (+ `.sha256`)
for the WPF client, with dependency notes from `versions.json` as needed).

Use a prerelease suffix (`wpf-v1.1.0-rc.1`) when marking a GitHub Release as a
prerelease. Prefer component tags (`interfaces-v*`, `utils-v*`, `core-v*`,
`wpf-v*`). Do not retag an existing version.

CI runs `.\scripts\Sync-Versions.ps1 -Check` so `versions.json`, `.csproj`, and
docs stay aligned.

## Commit messages

Write a short subject that states **why** the change exists (fix, add, update),
not a file list. Follow the recent history on `master`.
