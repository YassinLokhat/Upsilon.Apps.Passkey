using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using UnitTests.Presentation.Fakes;

namespace UnitTests.Presentation
{
   [TestClass]
   public sealed class JsonAppSettingsStoreTests
   {
      private string _directory = string.Empty;

      [TestInitialize]
      public void Initialize()
      {
         _directory = Path.Join(Path.GetTempPath(), $"passkey-settings-{Guid.NewGuid():N}");
         _ = Directory.CreateDirectory(_directory);
      }

      [TestCleanup]
      public void Cleanup()
      {
         AppServices.Reset();
         if (Directory.Exists(_directory))
         {
            Directory.Delete(_directory, recursive: true);
         }
      }

      [TestMethod]
      public void Load_MissingFile_WritesDefaultsAndReportsError()
      {
         FakeAppPaths paths = new(_directory);
         JsonAppSettingsStore store = new(paths);

         store.Load().Should().BeFalse();

         File.Exists(paths.ConfigFilePath).Should().BeTrue();
         store.Current.DefaultDatabaseDirectory.Should().Be(paths.DefaultDatabaseDirectory);
         store.Current.Language.Should().Be("System");
         store.Current.Theme.Should().Be(ThemePreferences.SystemCode);
      }

      [TestMethod]
      public void Load_InvalidJson_FallsBackToDefaults()
      {
         FakeAppPaths paths = new(_directory);
         File.WriteAllText(paths.ConfigFilePath, "{ not json");
         JsonAppSettingsStore store = new(paths);

         store.Load().Should().BeFalse();

         store.Current.LoginIdleTimeoutSeconds.Should().Be(5);
      }

      [TestMethod]
      public void SaveThenLoad_RoundTripsPreferences()
      {
         string filterPath = Path.Join(_directory, "pwned-ntlm.pkbf");
         FakeAppPaths paths = new(_directory) { OfflineLeakFilterPath = filterPath };
         JsonAppSettingsStore store = new(paths);
         store.Current.Language = "fr";
         store.Current.Theme = ThemePreferences.DarkCode;
         store.Current.LoginIdleTimeoutSeconds = 42;
         store.Current.LocalLeakDatabaseEnabled = true;
         store.Save();

         JsonAppSettingsStore reloaded = new(paths);
         reloaded.Load().Should().BeTrue();

         reloaded.Current.Language.Should().Be("fr");
         reloaded.Current.Theme.Should().Be(ThemePreferences.DarkCode);
         reloaded.Current.LoginIdleTimeoutSeconds.Should().Be(42);
         reloaded.Current.LocalLeakDatabaseEnabled.Should().BeTrue();
         reloaded.Current.LeakFilterConfig.FilterPath.Should().Be(filterPath);
      }

      [TestMethod]
      public void Load_HostWithoutOfflineFilter_DisablesIt()
      {
         FakeAppPaths paths = new(_directory);
         File.WriteAllText(paths.ConfigFilePath, """{ "LocalLeakDatabaseEnabled": true }""");
         JsonAppSettingsStore store = new(paths);

         store.Load().Should().BeTrue();

         store.Current.LocalLeakDatabaseEnabled.Should().BeFalse();
         store.Current.LeakFilterConfig.FilterPath.Should().BeEmpty();
      }

      [TestMethod]
      public void Load_KeepsCustomDatabaseDirectory()
      {
         FakeAppPaths paths = new(_directory);
         string custom = Path.Join(_directory, "custom");
         File.WriteAllText(paths.ConfigFilePath, $$"""{ "DefaultDatabaseDirectory": {{System.Text.Json.JsonSerializer.Serialize(custom)}} }""");
         JsonAppSettingsStore store = new(paths);

         store.Load().Should().BeTrue();

         store.Current.DefaultDatabaseDirectory.Should().Be(custom);
      }

      [TestMethod]
      public void HostCapabilities_PresetsDifferOnOfflineFilter()
      {
         HostCapabilities.Desktop.SupportsOfflineLeakFilter.Should().BeTrue();
         HostCapabilities.Mobile.SupportsOfflineLeakFilter.Should().BeFalse();
         AppServices.Capabilities.Should().Be(HostCapabilities.Desktop);
      }
   }
}
