# Testing and CI

## Automated tests

Two test projects:

| Project | TFM | Scope |
| ------- | --- | ----- |
| `UnitTests/Multiplateform/` | `net10.0` | Core + Utils (vault lifecycle, import/export, crypto, models). Asserts structured activity fields via `ExpectedActivity` — not localized UI strings. |
| `UnitTests/Windows/` | `net10.0-windows` | WPF ViewModels / localization through `AppServices` fakes. Localized activity rendering lives here (`ActivityAssertHelper`, `ActivityViewModelMessageTests`). |

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global hotkeys, and themed confirmation dialogs (`ThemedMessageBoxView`) stay on the [[WPF Client]] manual smoke list.

### Activity log assertions

Multiplateform tests compare `IActivity` fields with `UnitTestsHelper.LastActivitiesShouldMatch` and `ExpectedActivity` factories (`ImportFailed`, `DatabaseSaved`, `ItemUpdated`, …). Windows GUI / localization tests that need the Activities-grid message path use `ActivityAssertHelper` (same templates as `ActivityViewModel`). Alert waits use `WaitForAlertKind` / `WaitForAlerts` (event-driven); avoid `Thread.Sleep` polling.

### Naming conventions

- Models/Utils tests: `*UnitTests` classes and `CaseNN_…` method names.
- GUI tests: `*Tests` classes, descriptive method names, `[TestInitialize]` / `[TestCleanup]` via `GuiTestServices`.
- `TestDatabaseGenerator` is a manual fixture tool (not a `[TestMethod]` in the CI suite).

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Linux.slnx
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Windows.Gui"
```

### Coverage

`coverage.runsettings` measures **Core only** (the vault assembly). Utils (crypto, password factory) is a separate assembly and is not in that gate. The WPF assembly is excluded. Windows CI fails the build if line coverage of `Upsilon.Apps.Passkey.Core` drops below **90%**. Do not lower that gate without an explicit discussion in the pull request.

Locally, `run_code_coverage.bat` (and Windows CI) write TRX / Cobertura output under `_testResult/` (gitignored). The same path is set in `coverage.runsettings` (`<ResultsDirectory>`). Both test projects set `RunSettingsFilePath` to that file so Visual Studio Test Explorer and a plain `dotnet test` pick it up without a manual menu selection.

Linux CI builds Interfaces + Utils + Core and runs Multiplateform tests on `Upsilon.Apps.Passkey.Linux.slnx`.

## GitHub Actions

Windows and Linux build workflows run on push to `master` and on pull requests. CodeQL runs on **every** push (any branch) plus a weekly schedule (not on pull requests). A **Release** workflow runs when a version tag is pushed:

| Workflow | What it does |
| -------- | ------------ |
| `.github/workflows/csharp-dotnet-windows.yml` | Restore, **versions.json sync check**, Debug + Release build, tests with Cobertura, **90% Core line-coverage gate** |
| `.github/workflows/csharp-dotnet-linux.yml` | Restore, **versions.json sync check**, Debug + Release build of the Linux solution (Interfaces + Utils + Core + Multiplateform tests); `dotnet test` runs Multiplateform |
| `.github/workflows/codeql.yml` | CodeQL on a Release build of production projects (both test projects removed from the solution for the trace); weekly scan as well; SARIF filtered for `bin`/`obj`/`*.g.cs` |
| `.github/workflows/release.yml` | On `interfaces\|utils\|core\|wpf-v*.*.*` tags (legacy `v*` = WPF): sync check, Release build, tests, pack/publish via `scripts/Sync-Versions.ps1`, GitHub Release with dependency notes |

### Cutting a GitHub Release

1. Edit [`versions.json`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/versions.json) (version and dependency ranges for the packages you ship).
2. Run `.\scripts\Sync-Versions.ps1 -SyncOnly` and commit the updated `.csproj` / docs.
3. Merge to `master` and wait for Windows / Linux / CodeQL to pass.
4. Tag **each** package you ship and push the tags:

```bash
# Examples — use the versions from versions.json
git tag interfaces-v2.0.0 && git push origin interfaces-v2.0.0
git tag utils-v2.0.0 && git push origin utils-v2.0.0
git tag core-v2.0.0 && git push origin core-v2.0.0
git tag wpf-v2.0.0 && git push origin wpf-v2.0.0
```

The tag must match `versions.json` for that component. A `-` suffix marks the GitHub Release as a prerelease. Do not reuse a tag: `gh release create` will fail if that release already exists.

WPF assets are named `Upsilon.Apps.Passkey.GUI.WPF-{version}-win-x64.zip` (not a generic Passkey zip). Library releases attach a `.nupkg`. Each Release notes file lists dependency ranges from `versions.json`.

Local dry-run (all shippable packages into `_artifacts/`): `.\scripts\Sync-Versions.ps1`.

See [`CONTRIBUTING.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/CONTRIBUTING.md#cutting-a-release).

Dependabot is configured for the **.NET SDK** only (`dotnet-sdk` ecosystem). Test NuGet packages (MSTest, FluentAssertions) are not auto-bumped.

## What a change should add

* Tests for Core/Utils behaviour you change (crypto, vault lifecycle, import/export, alerts, persistence) — prefer Multiplateform when OS-independent
* ViewModel tests when you change GUI logic that already sits behind `AppServices` (Windows test project)
* README / SECURITY.md / `Wiki/` updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour

See [[Contributing]].
