# Testing and CI

## Automated tests

The `UnitTests` project covers Core and Utils (crypto, vault lifecycle, import/export, persistence, models) and GUI ViewModels (`UnitTests/Gui/`) through a replaceable `AppServices` seam and fakes (session, dialogs, clipboard). It references the WPF app and therefore uses a **Windows** TFM.

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global hotkeys, and themed confirmation dialogs (`ThemedMessageBoxView`) stay on the [[WPF Client]] manual smoke list.

### Activity log assertions

Import/export and persistence tests compare localized activity lines via `UnitTestsHelper.LastActivitiesShouldMatch`, which renders through `ActivityViewModel` (the same path as the WPF Activities grid). For import/export failures, prefer `UnitTestsHelper.FormatImportFailed(ImportExportError.…)` / `FormatExportFailed(…)` instead of hard-coded English strings. GUI localization coverage lives in `UnitTests/Gui/LocalizationTests.cs` (satellite key parity, `EnumDisplayHelper`, import/export failure messages).

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Gui"
```

### Coverage

`coverage.runsettings` measures **Core only** (the vault assembly). Utils (crypto, password factory) is a separate assembly and is not in that gate. The WPF assembly is excluded. Windows CI fails the build if line coverage of `Upsilon.Apps.Passkey.Core` drops below **90%**. Do not lower that gate without an explicit discussion in the pull request.

Locally, `run_code_coverage.bat` (and Windows CI) write TRX / Cobertura output under `_testResult/` (gitignored). The same path is set in `coverage.runsettings` (`<ResultsDirectory>`). `UnitTests.csproj` sets `RunSettingsFilePath` to that file so Visual Studio Test Explorer and a plain `dotnet test` on the test project pick it up without a manual menu selection.

Linux CI builds Interfaces + Utils + Core. The workflow still runs `dotnet test` on `Upsilon.Apps.Passkey.Linux.slnx`, but that solution has no test projects, so the step is effectively a no-op.

## GitHub Actions

Windows and Linux build workflows run on push to `master` and on pull requests. CodeQL runs on **every** push and pull request (any branch) plus a weekly schedule. A **Release** workflow runs when a version tag is pushed:

| Workflow | What it does |
| -------- | ------------ |
| `.github/workflows/csharp-dotnet-windows.yml` | Restore, **versions.json sync check**, Debug + Release build, tests with Cobertura, **90% Core line-coverage gate** |
| `.github/workflows/csharp-dotnet-linux.yml` | Restore, **versions.json sync check**, Debug + Release build of the Linux solution (Interfaces + Utils + Core); `dotnet test` with no test projects |
| `.github/workflows/codeql.yml` | CodeQL `security-and-quality` on a Release build of production projects (tests excluded); weekly scan as well |
| `.github/workflows/release.yml` | On `interfaces\|utils\|core\|wpf-v*.*.*` tags (legacy `v*` = WPF): sync check, Release build, tests, pack/publish via `scripts/Sync-Versions.ps1`, GitHub Release with dependency notes |

### Cutting a GitHub Release

1. Edit [`versions.json`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/versions.json) (version and dependency ranges for the packages you ship).
2. Run `.\scripts\Sync-Versions.ps1 -SyncOnly` and commit the updated `.csproj` / docs.
3. Merge to `master` and wait for Windows / Linux / CodeQL to pass.
4. Tag **each** package you ship and push the tags:

```bash
git checkout master
git pull
git tag wpf-v1.1.0
git push origin wpf-v1.1.0
```

The tag must match `versions.json` for that component. A `-` suffix marks the GitHub Release as a prerelease. Do not reuse a tag: `gh release create` will fail if that release already exists.

WPF assets are named `Upsilon.Apps.Passkey.GUI.WPF-{version}-win-x64.zip` (not a generic Passkey zip). Library releases attach a `.nupkg`. Each Release notes file lists dependency ranges from `versions.json`.

Local dry-run (all shippable packages into `_artifacts/`): `.\scripts\Sync-Versions.ps1`.

See [`CONTRIBUTING.md`](https://github.com/YassinLokhat/Upsilon.Apps.Passkey/blob/master/CONTRIBUTING.md#cutting-a-release).

Dependabot is configured for the **.NET SDK** only (`dotnet-sdk` ecosystem). Test NuGet packages (MSTest, FluentAssertions) are not auto-bumped.

## What a change should add

* Tests for Core/Utils behaviour you change (crypto, vault lifecycle, import/export, warnings, persistence)
* ViewModel tests when you change GUI logic that already sits behind `AppServices`
* README / SECURITY.md / `Wiki/` updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour

See [[Contributing]].
