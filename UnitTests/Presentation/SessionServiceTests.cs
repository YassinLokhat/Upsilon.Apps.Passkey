using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class SessionServiceTests
   {
      private const string _secretPath = @"C:\secret\vault-of-alice.pku";

      [TestInitialize]
      public void Initialize()
      {
         _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);
         AppServices.Reset();
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Clipboard = new FakeClipboardService();
         AppServices.PasswordFactory = UnitTestsHelper.FastPasswordFactory;
      }

      [TestCleanup]
      public void Cleanup()
      {
         AppServices.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void ErrorText_NeverExposesExceptionMessage()
      {
         Exception[] errors =
         [
            new UnauthorizedAccessException(_secretPath),
            new IOException(_secretPath),
            new CorruptedSourceException(_secretPath),
            new InvalidOperationException(_secretPath),
         ];

         foreach (Exception error in errors)
         {
            _ = ErrorText.Describe(error).Should().NotContain("secret").And.NotBeNullOrWhiteSpace();
            _ = ErrorText.ForLog(error).Should().NotContain("secret").And.StartWith(error.GetType().Name);
         }

         _ = ErrorText.Describe(new UnauthorizedAccessException()).Should().Be(Strings.Error_AccessDenied);
         _ = ErrorText.Describe(new IOException()).Should().Be(Strings.Error_FileAccess);
         _ = ErrorText.Describe(new CorruptedSourceException("x")).Should().Be(Strings.Error_InvalidVault);
         _ = ErrorText.Describe(new InvalidOperationException()).Should().Be(Strings.Error_Unexpected);
      }

      [TestMethod]
      public void AlertSummary_Empty_HasNoCountAndAllGroups()
      {
         AlertSummary summary = AlertSummary.Empty;

         _ = summary.TotalCount.Should().Be(0);
         _ = summary.OrderedGroups.Select(x => x.Group).Should().Equal(Enum.GetValues<AlertGroup>());
      }

      [TestMethod]
      public void AlertSummary_CountsHostIssuesAndPutsNonEmptyGroupsFirst()
      {
         AlertSummary summary = AlertSummary.From(
         [
            new HostSecuritySettingsAlert(HostSecurityIssue.IdleLoginDisabled | HostSecurityIssue.OfflineLeakFilterUnavailable),
         ]);

         _ = summary.TotalCount.Should().Be(2);
         _ = summary.Severity.Should().Be(AlertSeverity.Warning);
         _ = summary[AlertGroup.SecuritySettings].Count.Should().Be(2);
         _ = summary.OrderedGroups[0].Group.Should().Be(AlertGroup.SecuritySettings);
         _ = summary.OrderedGroups.Skip(1).Should().OnlyContain(x => x.Count == 0);
      }

      [TestMethod]
      public void ResolveEffective_UserOverrideWins()
      {
         _ = ThemePreferences.ResolveEffective("dark", null).Should().Be(ThemePreferences.DarkCode);
         _ = ThemePreferences.ResolveEffective(ThemePreferences.DarkCode, " LIGHT ").Should().Be(ThemePreferences.LightCode);
         _ = ThemePreferences.ResolveEffective("bogus", "bogus").Should().Be(ThemePreferences.SystemCode);
      }

      [TestMethod]
      public async Task WriteBack_ConflictAndFailure_ReportLocalizedPathFreeMessages()
      {
         using SessionService session = new();
         List<VaultWriteBackEventArgs> reports = [];
         session.WriteBackCompleted += (_, e) => reports.Add(e);

         FakeVaultSource conflict = new(VaultWriteBackResult.Conflict);
         FakeVaultSource failing = new(new IOException(_secretPath));
         FakeVaultSource written = new(VaultWriteBackResult.Written);

         foreach (FakeVaultSource source in new[] { conflict, failing, written })
         {
            IDatabase database = _createDatabase();
            session.TrackVaultSource(database, source);
            database.Close();
         }

         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();

         _ = reports.Should().HaveCount(2);
         _ = reports[0].Result.Should().Be(VaultWriteBackResult.Conflict);
         _ = reports[0].Message.Should().Be(Strings.Login_WriteBackConflict);
         _ = reports[1].Result.Should().Be(VaultWriteBackResult.Failed);
         _ = reports[1].Message.Should().NotContain("secret");
         _ = new[] { conflict, failing, written }.Should().OnlyContain(x => x.WriteCount == 1 && x.Disposed);
      }

      [TestMethod]
      public async Task WriteBack_RunsOneAtATimeInCloseOrder()
      {
         using SessionService session = new();
         TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
         List<string> order = [];
         FakeVaultSource first = new(VaultWriteBackResult.Written) { Gate = gate.Task, OnWrite = () => order.Add("first") };
         FakeVaultSource second = new(VaultWriteBackResult.Written) { OnWrite = () => order.Add("second") };

         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, first);
         database.Close();
         database = _createDatabase();
         session.TrackVaultSource(database, second);
         database.Close();

         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromMilliseconds(100))).Should().BeFalse();
         _ = second.WriteCount.Should().Be(0);

         gate.SetResult();
         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
         _ = order.Should().Equal("first", "second");
      }

      [TestMethod]
      public void Session_EndIsIdempotent_AndCopiedSecretsAreRemembered()
      {
         FakeClipboardService clipboard = (FakeClipboardService)AppServices.Clipboard!;
         using SessionService session = new();
         int changes = 0;
         session.SessionChanged += (_, _) => changes++;

         IDatabase database = _createDatabase();
         session.StartSession(database);
         _ = session.Database.Should().BeSameAs(database);

         Action copy = () => clipboard.RaiseSecretCopied("copied-secret");
         _ = copy.Should().NotThrow();

         session.EndSession();
         session.EndSession();

         _ = session.Database.Should().BeNull();
         _ = changes.Should().Be(2);
         _ = copy.Should().NotThrow();
      }

      [TestMethod]
      public async Task DeleteVault_DeletesSourceInsteadOfWritingBack()
      {
         using SessionService session = new();
         List<VaultWriteBackEventArgs> reports = [];
         session.WriteBackCompleted += (_, e) => reports.Add(e);

         FakeVaultSource source = new(VaultWriteBackResult.Written);
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         session.DeleteVault();
         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();

         _ = source.WriteCount.Should().Be(0);
         _ = source.DeleteCount.Should().Be(1);
         _ = source.Disposed.Should().BeTrue();
         _ = reports.Should().BeEmpty();
      }

      [TestMethod]
      public async Task DeleteVault_RefusedBySource_IsReported()
      {
         using SessionService session = new();
         List<VaultWriteBackEventArgs> reports = [];
         session.WriteBackCompleted += (_, e) => reports.Add(e);

         FakeVaultSource source = new(VaultWriteBackResult.Written) { DeleteResult = false };
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         session.DeleteVault();
         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();

         _ = reports.Should().ContainSingle().Which.Message.Should().Be(Strings.Login_VaultDeleteRefused);
      }

      [TestMethod]
      public async Task FlushToSource_WritesBackWhileSessionStaysOpen()
      {
         using SessionService session = new();
         _ = (await session.FlushToSourceAsync()).Should().BeNull();

         FakeVaultSource source = new(VaultWriteBackResult.Written);
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         _ = (await session.FlushToSourceAsync()).Should().Be(VaultWriteBackResult.Written);
         _ = source.WriteCount.Should().Be(1);
         _ = source.Disposed.Should().BeFalse();
         _ = session.Database.Should().BeSameAs(database);

         session.EndSession();
         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
         _ = source.WriteCount.Should().Be(2);
         _ = source.Disposed.Should().BeTrue();
      }

      [TestMethod]
      public async Task WriteBack_SourceDamaged_ReportsDedicatedMessage()
      {
         using SessionService session = new();
         List<VaultWriteBackEventArgs> reports = [];
         session.WriteBackCompleted += (_, e) => reports.Add(e);

         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, new FakeVaultSource(VaultWriteBackResult.SourceDamaged));
         database.Close();
         _ = (await session.FlushWriteBacksAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();

         _ = reports.Should().ContainSingle().Which.Message.Should().Be(Strings.Login_WriteBackSourceDamaged);
      }

      [TestMethod]
      public async Task DeleteVaultAsync_CompletesWithSourceResult()
      {
         using SessionService session = new();
         FakeVaultSource source = new(VaultWriteBackResult.Written) { DeleteResult = false };
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         _ = (await session.DeleteVaultAsync()).Should().BeFalse();
         _ = session.Database.Should().BeNull();
      }

      [TestMethod]
      public async Task EndSessionAndRename_WritesBackThenRenamesBeforeDispose()
      {
         using SessionService session = new();
         FakeVaultSource source = new(VaultWriteBackResult.Written) { RenameResult = true };
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         VaultRenameOutcome outcome = await session.EndSessionAndRenameAsync("renamed.pku");

         _ = outcome.Renamed.Should().BeTrue();
         _ = outcome.WriteBack.Should().Be(VaultWriteBackResult.Written);
         _ = outcome.Location.Should().Be("renamed.pku");
         _ = source.WriteCount.Should().Be(1);
         _ = source.DisposedBeforeRename.Should().BeFalse();
         _ = source.Disposed.Should().BeTrue();
         _ = session.Database.Should().BeNull();
      }

      [TestMethod]
      public async Task EndSessionAndRename_SkipsRenameWhenWriteBackFails()
      {
         using SessionService session = new();
         FakeVaultSource source = new(VaultWriteBackResult.Conflict) { RenameResult = true };
         IDatabase database = _createDatabase();
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         VaultRenameOutcome outcome = await session.EndSessionAndRenameAsync("renamed.pku");

         _ = outcome.Renamed.Should().BeFalse();
         _ = outcome.WriteBack.Should().Be(VaultWriteBackResult.Conflict);
         _ = source.RenamedTo.Should().BeNull();
         _ = source.Disposed.Should().BeTrue();
      }

      [TestMethod]
      public async Task FlushToSource_AfterConflict_StopsWritingForTheSession()
      {
         using SessionService session = new();
         IDatabase database = _createDatabase();
         FakeVaultSource source = new(VaultWriteBackResult.Conflict);
         session.TrackVaultSource(database, source);
         session.StartSession(database);

         (await session.FlushToSourceAsync()).Should().Be(VaultWriteBackResult.Conflict);
         (await session.FlushToSourceAsync()).Should().Be(VaultWriteBackResult.Conflict);

         source.WriteCount.Should().Be(1);
         session.EndSession();
      }

      private static IDatabase _createDatabase()
      {
         UnitTestsHelper.ClearTestEnvironment();
         return UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
      }

      private sealed class FakeVaultSource : IVaultSource
      {
         private readonly VaultWriteBackResult _result;
         private readonly Exception? _failure;

         public FakeVaultSource(VaultWriteBackResult result) => _result = result;

         public FakeVaultSource(Exception failure) => _failure = failure;

         public Task Gate { get; init; } = Task.CompletedTask;

         public Action? OnWrite { get; init; }

         public int WriteCount { get; private set; }

         public bool Disposed { get; private set; }

         public string WorkingPath => _secretPath;

         public string DisplayName => "vault.pku";

         public string Location => RenameResult && RenamedTo is not null ? RenamedTo : _secretPath;

         public bool DeleteResult { get; init; } = true;

         public int DeleteCount { get; private set; }

         public Task<VaultCheckoutResult> CheckoutAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(VaultCheckoutResult.Fresh);

         public Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
         {
            DeleteCount++;
            return Task.FromResult(DeleteResult);
         }

         public bool RenameResult { get; init; }

         public string? RenamedTo { get; private set; }

         public bool DisposedBeforeRename { get; private set; }

         public Task<bool> RenameAsync(string newFileName, CancellationToken cancellationToken = default)
         {
            DisposedBeforeRename = Disposed;
            RenamedTo = newFileName;
            return Task.FromResult(RenameResult);
         }

         public async Task<VaultWriteBackResult> WriteBackAsync(CancellationToken cancellationToken = default)
         {
            await Gate.ConfigureAwait(false);
            WriteCount++;
            OnWrite?.Invoke();
            return _failure is null ? _result : throw _failure;
         }

         public ValueTask DisposeAsync()
         {
            Disposed = true;
            return ValueTask.CompletedTask;
         }
      }
   }
}
