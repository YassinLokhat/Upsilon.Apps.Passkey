using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Helper;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Settings;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class SettingsViewModelsTests
   {
      [TestCleanup]
      public void Cleanup()
      {
         AppServices.Session.EndSession();
         AppServices.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public async Task ExportThenImport_GoesThroughFileExchangeStreams()
      {
         FakeFileExchange files = _startSession(out IDatabase source);
         _ = source.User!.AddService("Exported service");
         source.User.Settings.LogoutTimeout = 5;
         source.User.Settings.CleaningClipboardTimeout = 10;
         source.User.Settings.ShowPasswordDelay = 500;
         source.Save();

         using (UserSettingsViewModel vm = new())
         {
            await vm.ExportAsync(json: true);
         }

         files.WriteRequests.Should().ContainSingle().Which.Should().Be(".json");
         string exported = files.Written!;
         exported.Should().Contain("Exported service");

         AppServices.Session.EndSession();
         UnitTestsHelper.ClearTestEnvironment();
         files = _startSession(out IDatabase target);
         files.ReadContent = exported;
         files.ReadExtension = ".JSON";

         using (UserSettingsViewModel vm = new())
         {
            await vm.ImportAsync();
         }

         ((FakeDialogService)AppServices.Dialogs!).LastError.Should().BeNull();
         target.User!.Services.Select(s => s.ServiceName).Should().Contain("Exported service");
         files.LastReadStream!.CanRead.Should().BeFalse("the picked stream is disposed after import");
      }

      [TestMethod]
      public async Task Export_DeclinedPlaintextWarning_NeverOpensADocument()
      {
         FakeFileExchange files = _startSession(out _);
         ((FakeDialogService)AppServices.Dialogs!).ConfirmAnswer = DialogResult.No;

         using UserSettingsViewModel vm = new();
         await vm.ExportAsync(json: false);

         files.WriteRequests.Should().BeEmpty();
      }

      [TestMethod]
      public async Task Import_UnsupportedExtension_IsRejectedWithoutReading()
      {
         FakeFileExchange files = _startSession(out IDatabase database);
         files.ReadContent = "[]";
         files.ReadExtension = ".txt";

         using UserSettingsViewModel vm = new();
         await vm.ImportAsync();

         database.User!.Services.Should().BeEmpty();
         ((FakeDialogService)AppServices.Dialogs!).LastError.Should().Contain(EnumDisplayHelper.FormatFieldValue(nameof(ImportExportError), nameof(ImportExportError.ExtensionFileNotSupported)));
      }

      [TestMethod]
      /*
       * A new vault defaults to the directory configured in the app settings,
       * where the next login by username looks for it, not the host's folder.
      */
      public async Task NewUser_IsCreatedInTheConfiguredDirectory()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string configured = Path.GetFullPath(UnitTestsHelper.ComputeDatabaseFileDirectory());
         string hostDefault = Path.GetFullPath(Path.Join(UnitTestsHelper.ComputeTestDirectory(), "host-default"));
         FakeDialogService dialogs = new() { ConfirmAnswer = DialogResult.Yes };
         dialogs.ViewResults[typeof(ConfirmCredentialsRequest)] = true;
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Clipboard = new FakeClipboardService();
         AppServices.PasswordFactory = UnitTestsHelper.FastPasswordFactory;
         AppServices.Dialogs = dialogs;
         AppServices.Paths = new FakeAppPaths(hostDefault);
         AppServices.VaultSources = new PathVaultSourceFactory();
         FakeSettingsStore store = new();
         store.Current.DefaultDatabaseDirectory = configured;
         AppServices.Settings = store;

         string username = UnitTestsHelper.GetUsername();
         using (UserSettingsViewModel vm = new() { Username = username })
         {
            vm.Passkeys.Entries[0].Attach(new FakeSecretInput { Value = "first passkey" });
            await vm.SaveAsync();
         }

         dialogs.LastError.Should().BeNull();
         string fileName = AppServices.Cryptography.GetHash(username) + ".pku";
         File.Exists(Path.Join(configured, fileName)).Should().BeTrue();
         File.Exists(Path.Join(hostDefault, fileName)).Should().BeFalse();

         UnitTestsHelper.ClearTestEnvironment();
      }

      private static FakeFileExchange _startSession(out IDatabase database)
      {
         UnitTestsHelper.ClearTestEnvironment();
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Clipboard = new FakeClipboardService();
         AppServices.PasswordFactory = UnitTestsHelper.FastPasswordFactory;
         AppServices.Dialogs = new FakeDialogService { ConfirmAnswer = DialogResult.Yes };
         FakeFileExchange files = new();
         AppServices.Files = files;
         database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
         AppServices.Session.StartSession(database);
         return files;
      }

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
      public void PasskeyEntry_ReattachToNewControl_MovesTypedSecretAndWipesOldOne()
      {
         using PasskeyEntryViewModel entry = new(seed: null, storedIndex: null);
         FakeSecretInput first = new();
         FakeSecretInput second = new();
         entry.Attach(first);
         first.Value = "typed";

         entry.Attach(second);

         second.Value.Should().Be("typed");
         first.Value.Should().BeEmpty();
         entry.Reveal().Should().Be("typed");
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

      private sealed class FakeFileExchange : IFileExchange
      {
         public string ReadContent { get; set; } = string.Empty;

         public string ReadExtension { get; set; } = ".json";

         public MemoryStream? LastReadStream { get; private set; }

         public List<string> WriteRequests { get; } = [];

         public string? Written { get; private set; }

         public Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default)
         {
            LastReadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadContent));
            return Task.FromResult<ExchangeFile?>(new ExchangeFile(LastReadStream, ReadExtension, "picked" + ReadExtension));
         }

         public Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default)
         {
            WriteRequests.Add(extension);
            CapturingStream stream = new(text => Written = text);
            return Task.FromResult<ExchangeFile?>(new ExchangeFile(stream, extension, suggestedFileName));
         }

         private sealed class CapturingStream(Action<string> onDispose) : MemoryStream
         {
            protected override void Dispose(bool disposing)
            {
               if (disposing)
               {
                  onDispose(System.Text.Encoding.UTF8.GetString(ToArray()));
               }

               base.Dispose(disposing);
            }
         }
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
