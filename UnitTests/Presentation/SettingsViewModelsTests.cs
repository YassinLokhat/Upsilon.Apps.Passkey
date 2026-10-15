using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class SettingsViewModelsTests
   {
      [TestCleanup]
      public void Cleanup() => AppServices.Reset();

      [TestMethod]
      public void NormalizeUsername_TrimsAndHandlesNull()
      {
         CredentialRules.NormalizeUsername("  bob ").Should().Be("bob");
         CredentialRules.NormalizeUsername(null).Should().BeEmpty();
      }

      [TestMethod]
      public void PasskeyList_WithoutUser_StartsWithOneBlankRow_AndValidates()
      {
         using PasskeyListViewModel list = new(user: null);

         list.Entries.Should().ContainSingle();
         list.Entries[0].StoredIndex.Should().BeNull();
         list.ValidationError().Should().Be(Strings.Msg_NoPasswordEmpty);

         FakeSecretInput input = new();
         list.Entries[0].Attach(input);
         input.Value = "first";

         list.ValidationError().Should().BeEmpty();
         list.RevealAll().Should().Equal("first");
      }

      [TestMethod]
      public void PasskeyList_AddMoveRemove_KeepsIndexesAndAtLeastOneRow()
      {
         using PasskeyListViewModel list = new(user: null);
         PasskeyEntryViewModel first = list.Entries[0];
         PasskeyEntryViewModel? added = null;
         list.EntryAdded += (_, e) => added = e;

         PasskeyEntryViewModel second = list.Add();
         added.Should().BeSameAs(second);

         list.Move(second, -1).Should().BeTrue();
         list.Entries.Should().Equal(second, first);
         second.Index.Should().Be(0);
         first.Index.Should().Be(1);
         list.Move(second, -1).Should().BeFalse();

         second.DeleteCommand.Execute(null);
         list.Entries.Should().Equal(first);
         first.Index.Should().Be(0);

         list.Remove(first).Should().BeFalse();
         list.Entries.Should().ContainSingle();
      }

      [TestMethod]
      public void PasskeyEntry_Attach_MovesSeedIntoControl_AndClearWipesIt()
      {
         PasskeyEntryViewModel entry = new(AppServices.SecretProtector.Protect("stored"), storedIndex: 0);
         entry.Reveal().Should().Be("stored");

         FakeSecretInput input = new();
         entry.Attach(input);
         input.Value.Should().Be("stored");

         input.Value = "edited";
         entry.Reveal().Should().Be("edited");

         entry.Dispose();
         input.Value.Should().BeEmpty();
      }

      [TestMethod]
      public void AppSettings_EditsApplyOnlyOnSave()
      {
         FakeSettingsStore store = new();
         AppServices.Settings = store;
         store.Current.LoginIdleTimeoutSeconds = 30;

         AppSettingsViewModel vm = new();
         vm.LoginIdleTimeoutSeconds.Should().Be(30);

         vm.LoginIdleTimeoutSeconds = 0;
         vm.SelectedTheme = ThemePreferences.GetOptionOrDefault(ThemePreferences.DarkCode);
         store.Current.LoginIdleTimeoutSeconds.Should().Be(30);
         store.SaveCount.Should().Be(0);

         _ = vm.Save();

         store.Current.LoginIdleTimeoutSeconds.Should().Be(0);
         store.Current.Theme.Should().Be(ThemePreferences.DarkCode);
         store.SaveCount.Should().Be(1);
      }

      [TestMethod]
      public void AppSettings_NegativeValuesClampToZero_AndResetRestoresDefaults()
      {
         FakeSettingsStore store = new();
         AppServices.Settings = store;

         AppSettingsViewModel vm = new()
         {
            LoginIdleTimeoutSeconds = -5,
            OfflineLeakFilterAutoUpdateFrequency = -1,
         };

         vm.LoginIdleTimeoutSeconds.Should().Be(0);
         vm.OfflineLeakFilterAutoUpdateFrequency.Should().Be(0);

         vm.Reset();

         vm.LoginIdleTimeoutSeconds.Should().Be(new AppSettings().LoginIdleTimeoutSeconds);
         store.SaveCount.Should().Be(1);
      }

      [TestMethod]
      public void ThemeOptions_UnknownCodeFallsBackToSystem()
      {
         ThemePreferences.Options.Select(o => o.Code).Should().Equal(ThemePreferences.SystemCode, ThemePreferences.LightCode, ThemePreferences.DarkCode);
         ThemePreferences.GetOptionOrDefault("nope").Code.Should().Be(ThemePreferences.SystemCode);
      }

      private sealed class FakeSecretInput : ISecretInput
      {
         public string Value { get; set; } = string.Empty;

         public string Read() => Value;

         public void Write(string secret) => Value = secret;

         public void Clear() => Value = string.Empty;
      }

      private sealed class FakeSettingsStore : IAppSettingsStore
      {
         public AppSettings Current { get; set; } = CreateDefaultSettings();

         public string ConfigFilePath => "config.json";

         public int SaveCount { get; private set; }

         public bool Load() => true;

         public void Save() => SaveCount++;

         public AppSettings CreateDefault() => CreateDefaultSettings();

         private static AppSettings CreateDefaultSettings() => new() { DefaultDatabaseDirectory = Path.GetTempPath() };
      }
   }
}
