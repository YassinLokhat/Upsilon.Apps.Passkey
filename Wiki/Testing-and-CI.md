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

Linux CI builds Interfaces + Utils + Core. The workflow still runs `dotnet test` on `Upsilon.Apps.Passkey.Linux.slnx`, but that solution has no test projects, so the step is effectively a no-op.

## GitHub Actions

Windows and Linux build workflows run on push to `master` and on pull requests. CodeQL runs on **every** push and pull request (any branch) plus a weekly schedule:

| Workflow | What it does |
| -------- | ------------ |
| `.github/workflows/csharp-dotnet-windows.yml` | Restore, Debug + Release build, tests with Cobertura, **90% Core line-coverage gate** |
| `.github/workflows/csharp-dotnet-linux.yml` | Restore and Debug + Release build of the Linux solution (Interfaces + Utils + Core); `dotnet test` with no test projects |
| `.github/workflows/codeql.yml` | CodeQL `security-and-quality` on a Release build of production projects (tests excluded); weekly scan as well |

Dependabot is configured for the **.NET SDK** only (`dotnet-sdk` ecosystem). Test NuGet packages (MSTest, FluentAssertions) are not auto-bumped.

## What a change should add

* Tests for Core/Utils behaviour you change (crypto, vault lifecycle, import/export, warnings, persistence)
* ViewModel tests when you change GUI logic that already sits behind `AppServices`
* README / SECURITY.md / `Wiki/` updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour

See [[Contributing]].
