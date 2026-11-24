using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class HardeningTests
   {
      private DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

      [TestInitialize]
      public void Initialize()
      {
         _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
         AppServices.Reset();
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Clipboard = new FakeClipboardService();
         AppServices.PasswordFactory = UnitTestsHelper.FastPasswordFactory;
         AppServices.Dialogs = new FakeDialogService();
      }

      [TestCleanup]
      public void Cleanup()
      {
         AppServices.Session.EndSession();
         AppServices.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void BackgroundLock_EndsSessionOnlyAfterTheThreshold()
      {
         FakeLifecycle lifecycle = new();
         using BackgroundLockPolicy policy = new(lifecycle, () => _now);
         IDatabase database = _startSession();

         lifecycle.Background();
         _now = _now.AddSeconds(AppSettings.DEFAULT_BACKGROUND_LOCK_SECONDS - 1);
         lifecycle.Resume();
         AppServices.Session.Database.Should().BeSameAs(database);

         lifecycle.Background();
         _now = _now.AddSeconds(AppSettings.DEFAULT_BACKGROUND_LOCK_SECONDS);
         lifecycle.Resume();
         AppServices.Session.Database.Should().BeNull("the vault is hidden at once, before any upload completes");
      }

      [TestMethod]
      public void BackgroundLock_ZeroLocksOnEveryReturn_AndIgnoresResumeWithoutBackground()
      {
         FakeLifecycle lifecycle = new();
         using BackgroundLockPolicy policy = new(lifecycle, () => _now);
         AppServices.Settings = new InMemorySettingsStore(new AppSettings { BackgroundLockSeconds = 0 });
         _ = _startSession();

         lifecycle.Resume();
         AppServices.Session.Database.Should().NotBeNull();

         lifecycle.Background();
         lifecycle.Resume();
         AppServices.Session.Database.Should().BeNull();
      }

      [TestMethod]
      public async Task Login_SavesConflictCopiesAndDeletesOnlyTheSavedOnes()
      {
         FakeConflictStore store = new("a-conflict.pku", "b-conflict.pku");
         FakeExchange files = new() { CancelAfter = 1 };
         AppServices.ConflictCopies = store;
         AppServices.Files = files;

         using LoginViewModel vm = new();
         vm.ConflictCopyCount.Should().Be(2);
         vm.ConflictCopiesMessage.Should().Contain("2");

         await vm.SaveConflictCopiesAsync();

         files.Saved.Should().ContainSingle().Which.Should().Be("a-conflict.pku");
         store.Remaining.Should().Equal("b-conflict.pku");
         vm.ConflictCopyCount.Should().Be(1);
      }

      private static IDatabase _startSession()
      {
         UnitTestsHelper.ClearTestEnvironment();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
         AppServices.Session.StartSession(database);
         return database;
      }

      private sealed class FakeLifecycle : IAppLifecycle
      {
         public event EventHandler? EnteredBackground;
         public event EventHandler? Resumed;

         public void Background() => EnteredBackground?.Invoke(this, EventArgs.Empty);

         public void Resume() => Resumed?.Invoke(this, EventArgs.Empty);
      }

      private sealed class InMemorySettingsStore(AppSettings current) : IAppSettingsStore
      {
         public AppSettings Current { get; set; } = current;

         public string ConfigFilePath => "config.json";

         public bool Load() => true;

         public void Save()
         {
         }

         public AppSettings CreateDefault() => new();
      }

      private sealed class FakeConflictStore(params string[] names) : IConflictCopyStore
      {
         private readonly List<string> _names = [.. names];

         public IReadOnlyList<string> Remaining => _names;

         public IReadOnlyList<ConflictCopy> List() => [.. _names.Select(n => new ConflictCopy(n, n, DateTime.UtcNow))];

         public Stream OpenRead(ConflictCopy copy) => new MemoryStream([1, 2, 3]);

         public void Delete(ConflictCopy copy) => _names.Remove(copy.Id);
      }

      private sealed class FakeExchange : IFileExchange
      {
         public int CancelAfter { get; init; } = int.MaxValue;

         public List<string> Saved { get; } = [];

         public Task<ExchangeFile?> OpenReadAsync(string title, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default)
            => Task.FromResult<ExchangeFile?>(null);

         public Task<ExchangeFile?> OpenWriteAsync(string title, string suggestedFileName, string extension, CancellationToken cancellationToken = default)
         {
            if (Saved.Count >= CancelAfter)
            {
               return Task.FromResult<ExchangeFile?>(null);
            }

            Saved.Add(suggestedFileName);
            return Task.FromResult<ExchangeFile?>(new ExchangeFile(new MemoryStream(), extension, suggestedFileName));
         }
      }
   }
}
