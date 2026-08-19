# Testing and CI

## Automated tests

The `UnitTests` project covers Core (crypto, vault lifecycle, import/export, persistence, models) and GUI ViewModels (`UnitTests/Gui/`) through a replaceable `AppServices` seam and fakes (session, dialogs, clipboard). It references the WPF app and therefore uses a **Windows** TFM.

There is no UI automation (FlaUI / WinAppDriver). Login `PasswordBox`, global hotkeys, and real MessageBoxes stay on the [[WPF Client]] manual smoke list.

```bash
dotnet test Upsilon.Apps.Passkey.Windows.slnx --settings coverage.runsettings
dotnet test Upsilon.Apps.Passkey.Windows.slnx --filter "FullyQualifiedName~UnitTests.Gui"
```

### Coverage

`coverage.runsettings` measures **Core only**. The WPF assembly is excluded. Windows CI fails the build if line coverage of `Upsilon.Apps.Passkey.Core` drops below **90%**. Do not lower that gate without an explicit discussion in the pull request.

Linux CI builds Interfaces + Core only; it does not run tests.

## GitHub Actions

Workflows run on `master` and pull requests:

| Workflow | What it does |
| -------- | ------------ |
| `.github/workflows/csharp-dotnet-windows.yml` | Restore, Debug + Release build, tests with Cobertura, **90% Core line-coverage gate** |
| `.github/workflows/csharp-dotnet-linux.yml` | Restore and Debug + Release build of the Linux solution (Core + Interfaces) |
| `.github/workflows/codeql.yml` | CodeQL `security-and-quality` on a Release build of production projects (tests excluded); weekly scan as well |

Dependabot is configured for the **.NET SDK** only (`dotnet-sdk` ecosystem). Test NuGet packages (MSTest, FluentAssertions) are not auto-bumped.

## What a change should add

* Tests for Core behaviour you change (crypto, vault lifecycle, import/export, warnings, persistence)
* ViewModel tests when you change GUI logic that already sits behind `AppServices`
* README / SECURITY.md / `Wiki/` updates when you change a public contract, a threat-model assumption, or a user-visible security behaviour

See [[Contributing]].
