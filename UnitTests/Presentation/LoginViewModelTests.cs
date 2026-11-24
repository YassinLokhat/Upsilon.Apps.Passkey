using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels;
using Upsilon.Apps.Passkey.Interfaces.Events;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class LoginViewModelTests
   {
      [TestInitialize]
      public void Initialize() => _ = LocalizationService.Apply(LocalizationService.DefaultLanguageCode);

      [TestCleanup]
      public void Cleanup() => AppServices.Reset();

      [TestMethod]
      /*
       * Without a picked vault, the username resolves to {hash}.pku in the
       * default directory; a missing file is reported and nothing is opened.
      */
      public async Task Case01_DefaultDirectory_MissingVault_ReportsAndStaysClosed()
      {
         UnitTestsHelper.ClearTestEnvironment();
         _configureHost(UnitTestsHelper.ComputeTestDirectory());
         LoginViewModel viewModel = new();

         await viewModel.SubmitUsernameAsync("   ");
         _ = viewModel.StatusMessage.Should().BeEmpty();

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());

         _ = viewModel.DatabaseOpened.Should().BeFalse();
         _ = viewModel.IsBusy.Should().BeFalse();
         _ = viewModel.StatusMessage.Should().Be(Strings.Format(
            nameof(Strings.Login_NoVaultForUsername),
            UnitTestsHelper.ComputeTestDirectory(),
            Strings.Login_OpenVault));

         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Username then ordered passkeys from the default directory: LoggedIn is
       * raised on the last passkey, and closing the database raises SessionEnded
       * and clears the session state.
      */
      public async Task Case02_DefaultDirectory_ProgressiveLogin_ThenSessionEnded()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         UnitTestsHelper.CreateTestDatabase(passkeys).Close();
         _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory());

         LoginViewModel viewModel = new();
         int loggedIn = 0;
         LogoutEventArgs? ended = null;
         viewModel.LoggedIn += (_, _) => loggedIn++;
         viewModel.SessionEnded += (_, e) => ended = e;

         await viewModel.SubmitUsernameAsync($"  {UnitTestsHelper.GetUsername()}  ");
         _ = viewModel.DatabaseOpened.Should().BeTrue();
         _ = viewModel.CredentialsLabel.Should().Be(Strings.Label_Password);
         _ = viewModel.StatusMessage.Should().Be(Strings.Login_EnterPasskeys);

         for (int i = 0; i < passkeys.Length - 1; i++)
         {
            _ = (await viewModel.SubmitPasskeyAsync(passkeys[i])).Should().BeFalse();
         }

         _ = (await viewModel.SubmitPasskeyAsync(passkeys[^1])).Should().BeTrue();
         _ = loggedIn.Should().Be(1);
         _ = viewModel.IsLoggedIn.Should().BeTrue();
         _ = viewModel.User.Should().NotBeNull();

         viewModel.Database!.Close();

         _ = ended.Should().NotBeNull();
         _ = ended!.LoginTimeoutReached.Should().BeFalse();
         _ = viewModel.DatabaseOpened.Should().BeFalse();
         _ = viewModel.IsLoggedIn.Should().BeFalse();

         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * An explicitly picked vault wins over the default directory, which here
       * does not contain the vault at all.
      */
      public async Task Case03_PickedVault_WinsOverDefaultDirectory()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         UnitTestsHelper.CreateTestDatabase(passkeys).Close();
         string emptyDirectory = Path.Join(UnitTestsHelper.ComputeTestDirectory(), "empty");
         _ = Directory.CreateDirectory(emptyDirectory);
         _configureHost(emptyDirectory);

         LoginViewModel viewModel = new();
         viewModel.SelectVault(Path.GetFullPath(UnitTestsHelper.ComputeDatabaseFilePath()), "test.pku");
         _ = viewModel.HasSelectedVault.Should().BeTrue();
         _ = viewModel.SelectedVaultName.Should().Be("test.pku");

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         _ = viewModel.DatabaseOpened.Should().BeTrue();

         foreach (string passkey in passkeys)
         {
            _ = await viewModel.SubmitPasskeyAsync(passkey);
         }

         _ = viewModel.IsLoggedIn.Should().BeTrue();

         viewModel.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A wrong passkey is not reported as such (no rollback) and does not log
       * in. Reset closes the half-open session, forgets the picked vault and
       * releases the file so the same vault can be opened again.
      */
      public async Task Case04_WrongPasskey_ThenReset_ReleasesVault()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         UnitTestsHelper.CreateTestDatabase(passkeys).Close();
         _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory());
         string username = UnitTestsHelper.GetUsername();

         LoginViewModel viewModel = new();
         viewModel.SelectVault(UnitTestsHelper.ComputeDatabaseFilePath(), "picked.pku");
         await viewModel.SubmitUsernameAsync(username);

         _ = (await viewModel.SubmitPasskeyAsync(UnitTestsHelper.GetRandomString())).Should().BeFalse();
         _ = viewModel.IsLoggedIn.Should().BeFalse();
         _ = viewModel.DatabaseOpened.Should().BeTrue();

         viewModel.Reset();
         _ = viewModel.DatabaseOpened.Should().BeFalse();
         _ = viewModel.HasSelectedVault.Should().BeFalse();
         _ = viewModel.StatusMessage.Should().BeEmpty();

         await viewModel.SubmitUsernameAsync(username);
         _ = viewModel.DatabaseOpened.Should().BeTrue();

         viewModel.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      [DataRow((int)DialogResult.Yes, true, false)]
      [DataRow((int)DialogResult.No, false, false)]
      [DataRow((int)DialogResult.Cancel, true, true)]
      /*
       * Unsaved edits from a previous session raise AutoSaveDetected during
       * login. The answer maps to Yes = merge and save, No = discard,
       * Cancel = merge without saving (still pending).
      */
      public async Task Case05_AutoSavePrompt_MapsAnswerToMergeBehavior(int answerValue, bool merged, bool stillPending)
      {
         DialogResult answer = (DialogResult)answerValue;
         string testName = $"{nameof(Case05_AutoSavePrompt_MapsAnswerToMergeBehavior)}_{answer}";
         UnitTestsHelper.ClearTestEnvironment(testName);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();

         IDatabase previous = UnitTestsHelper.CreateTestDatabase(passkeys, testName);
         _ = previous.User!.AddService("PendingService");
         previous.Close();

         FakeDialogService dialogs = _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory(testName));
         dialogs.ConfirmAnswer = answer;

         LoginViewModel viewModel = new();
         await viewModel.SubmitUsernameAsync(testName);
         foreach (string passkey in passkeys)
         {
            _ = await viewModel.SubmitPasskeyAsync(passkey);
         }

         _ = viewModel.IsLoggedIn.Should().BeTrue();
         _ = dialogs.ConfirmCount.Should().Be(1);
         _ = viewModel.User!.Services.Any(s => s.ServiceName == "PendingService").Should().Be(merged);
         _ = viewModel.User.HasChanged().Should().Be(stillPending);

         viewModel.Reset();
         UnitTestsHelper.ClearTestEnvironment(testName);
      }

      [TestMethod]
      /*
       * Usernames resolve against the default database directory configured in
       * the app settings, not the host's built-in folder.
      */
      public async Task Case06_ConfiguredDirectory_WinsOverHostDefault()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         UnitTestsHelper.CreateTestDatabase(passkeys).Close();
         string hostDefault = Path.Join(UnitTestsHelper.ComputeTestDirectory(), "host-default");
         _ = Directory.CreateDirectory(hostDefault);
         _configureHost(hostDefault);
         AppServices.Settings = new JsonAppSettingsStore(AppServices.Paths!);
         AppServices.Settings.Current.DefaultDatabaseDirectory = UnitTestsHelper.ComputeDatabaseFileDirectory();

         _ = AppServices.DefaultVaultDirectory.Should().Be(UnitTestsHelper.ComputeDatabaseFileDirectory());

         LoginViewModel viewModel = new();
         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         _ = viewModel.DatabaseOpened.Should().BeTrue();

         viewModel.Reset();
         AppServices.Settings.Current.DefaultDatabaseDirectory = " ";
         _ = AppServices.DefaultVaultDirectory.Should().Be(hostDefault);

         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Once the vault is open, the idle countdown shows in the title and, at
       * zero, closes the half-open session and asks the host to clear its field.
       * A tick queued before the reset is ignored.
      */
      public async Task Case07_IdleTimeout_CountsDownInTitle_ThenResetsHalfOpenLogin()
      {
         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray()).Close();
         _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory());
         FakeTimerFactory timers = _configureIdleTimeout(seconds: 2);

         LoginViewModel viewModel = new();
         FakeTimer timer = timers.Last!;
         int cleared = 0;
         viewModel.IdleTimeoutElapsed += (_, _) => cleared++;
         _ = viewModel.Title.Should().Be(AppServices.Info.Title);

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         _ = viewModel.DatabaseOpened.Should().BeTrue();
         _ = timer.IsRunning.Should().BeTrue();
         _ = timer.Interval.Should().Be(TimeSpan.FromSeconds(1));
         _ = viewModel.Title.Should().Be(AppServices.Info.Title + Strings.Format(nameof(Strings.Title_IdleResetCredentialTimeout), 2));

         timer.Fire();
         _ = viewModel.Title.Should().Be(AppServices.Info.Title + Strings.Format(nameof(Strings.Title_IdleResetCredentialTimeout), 1));

         timer.Fire();
         timer.Fire();
         _ = cleared.Should().Be(1);
         _ = viewModel.DatabaseOpened.Should().BeFalse();
         _ = timer.IsRunning.Should().BeFalse();
         _ = viewModel.Title.Should().Be(AppServices.Info.Title);

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         _ = viewModel.DatabaseOpened.Should().BeTrue("the reset released the vault");

         viewModel.Reset();
         viewModel.Dispose();
         _ = timer.IsDisposed.Should().BeTrue();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Typing restarts the countdown; an emptied field only does so while a
       * vault is half-open. A zero timeout disables it.
      */
      public void Case08_CredentialInput_RestartsCountdown_UnlessDisabled()
      {
         _configureHost(UnitTestsHelper.ComputeTestDirectory());
         FakeTimerFactory timers = _configureIdleTimeout(seconds: 5);
         using LoginViewModel viewModel = new();
         FakeTimer timer = timers.Last!;

         viewModel.NotifyCredentialInput(hasText: false);
         _ = timer.IsRunning.Should().BeFalse();

         viewModel.NotifyCredentialInput(hasText: true);
         _ = timer.IsRunning.Should().BeTrue();
         timer.Fire();
         viewModel.NotifyCredentialInput(hasText: true);
         _ = viewModel.Title.Should().EndWith(Strings.Format(nameof(Strings.Title_IdleResetCredentialTimeout), 5));

         viewModel.Reset();
         _ = timer.IsRunning.Should().BeFalse();

         AppServices.Settings!.Current.LoginIdleTimeoutSeconds = 0;
         viewModel.NotifyCredentialInput(hasText: true);
         _ = timer.IsRunning.Should().BeFalse();
         _ = viewModel.Title.Should().Be(AppServices.Info.Title);
      }

      [TestMethod]
      /*
       * New user, app settings and the generator open their views from the login
       * screen, but not while a vault is half-open (same rule as the WPF menus).
      */
      public async Task Case09_Menus_ShowViews_ButNotDuringThePasskeyPrompt()
      {
         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray()).Close();
         FakeDialogService dialogs = _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory());
         FakeTimerFactory timers = _configureIdleTimeout(seconds: 5);
         LoginViewModel viewModel = new();
         FakeTimer timer = timers.Last!;

         _ = viewModel.MenusEnabled.Should().BeTrue();
         viewModel.NotifyCredentialInput(hasText: true);
         await viewModel.NewUserCommand.ExecuteAsync(null);
         _ = timer.IsRunning.Should().BeFalse("a dialog pauses the idle reset");
         await viewModel.AppSettingsCommand.ExecuteAsync(null);
         await viewModel.GeneratePasswordCommand.ExecuteAsync(null);
         _ = dialogs.ShownRequests.Select(r => r.GetType()).Should().Equal(
            typeof(UserSettingsRequest),
            typeof(AppSettingsRequest),
            typeof(GeneratePasswordRequest));

         List<string?> changed = [];
         viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());

         _ = changed.Should().Contain(nameof(LoginViewModel.MenusEnabled));
         _ = viewModel.MenusEnabled.Should().BeFalse();
         _ = viewModel.NewUserCommand.CanExecute(null).Should().BeFalse();
         _ = viewModel.AppSettingsCommand.CanExecute(null).Should().BeFalse();
         _ = viewModel.GeneratePasswordCommand.CanExecute(null).Should().BeFalse();

         viewModel.Reset();
         _ = viewModel.MenusEnabled.Should().BeTrue();
         _ = viewModel.NewUserCommand.CanExecute(null).Should().BeTrue();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Launch arguments [vault.pku] [username] select the vault and submit the
       * username. Anything but an existing local .pku file is ignored.
      */
      public async Task Case10_CommandLine_SelectsVaultAndSubmitsUsername()
      {
         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray()).Close();
         string emptyDirectory = Path.Join(UnitTestsHelper.ComputeTestDirectory(), "empty");
         _ = Directory.CreateDirectory(emptyDirectory);
         _configureHost(emptyDirectory);
         string vault = Path.GetFullPath(UnitTestsHelper.ComputeDatabaseFilePath());
         string notes = Path.Join(emptyDirectory, "notes.txt");
         File.WriteAllText(notes, "not a vault");

         _ = LoginCommandLine.Parse(["exe"]).Should().Be(((string?)null, (string?)null));
         _ = LoginCommandLine.Parse(["exe", notes]).VaultPath.Should().BeNull();
         _ = LoginCommandLine.Parse(["exe", Path.Join(emptyDirectory, "missing.pku")]).VaultPath.Should().BeNull();
         _ = LoginCommandLine.Parse(["exe", @"\\server\share\vault.pku"]).VaultPath.Should().BeNull();
         _ = LoginCommandLine.Parse(["exe", vault, "bob"]).Should().Be((vault, "bob"));

         LoginViewModel viewModel = new();
         await viewModel.ApplyCommandLineAsync(["exe", vault, UnitTestsHelper.GetUsername()]);

         _ = viewModel.SelectedVaultName.Should().Be(Path.GetFileName(vault));
         _ = viewModel.DatabaseOpened.Should().BeTrue();

         viewModel.Reset();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Open picks a vault through the dialog service and selects it, which
       * resets the credentials first; a cancelled picker changes nothing.
      */
      public async Task Case11_OpenVaultCommand_SelectsPickedVault_CancelKeepsState()
      {
         FakeDialogService dialogs = _configureHost(UnitTestsHelper.ComputeTestDirectory());
         using LoginViewModel viewModel = new();
         int resets = 0;
         viewModel.CredentialsReset += (_, _) => resets++;

         await viewModel.OpenVaultCommand.ExecuteAsync(null);
         _ = viewModel.HasSelectedVault.Should().BeFalse();
         _ = resets.Should().Be(0);

         string vault = Path.Join(UnitTestsHelper.ComputeTestDirectory(), "picked.pku");
         dialogs.OpenFileAnswer = vault;
         await viewModel.OpenVaultCommand.ExecuteAsync(null);

         _ = viewModel.SelectedVaultName.Should().Be("picked.pku");
         _ = viewModel.DatabaseLabel.Should().Be(Strings.Format(nameof(Strings.Msg_DatabaseLabel), "picked.pku"));
         _ = resets.Should().Be(1);
      }

      [TestMethod]
      /*
       * Each failure shown in StatusMessage is also raised through
       * ErrorReported, for hosts that report failures in a dialog.
      */
      public async Task Case12_Failures_RaiseErrorReported()
      {
         UnitTestsHelper.ClearTestEnvironment();
         _configureHost(UnitTestsHelper.ComputeTestDirectory());
         using LoginViewModel viewModel = new();
         List<LoginErrorEventArgs> errors = [];
         viewModel.ErrorReported += (_, e) => errors.Add(e);

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         _ = errors.Should().ContainSingle();
         _ = errors[0].Title.Should().Be(Strings.Title_Error);
         _ = errors[0].Message.Should().Be(viewModel.StatusMessage);

         AppServices.Clipboard = null;
         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());

         _ = errors.Should().HaveCount(2);
         _ = errors[1].Message.Should().Be(Strings.Login_HostNotConfigured);
         _ = viewModel.StatusMessage.Should().Be(Strings.Login_HostNotConfigured);

         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * The database label names the half-open vault (default directory or
       * picked), falls back to "no database" on reset, and follows the language.
      */
      public async Task Case13_DatabaseLabel_FollowsVault_AndLanguage()
      {
         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray()).Close();
         _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory());
         using LoginViewModel viewModel = new();
         _ = viewModel.Should().BeAssignableTo<ILanguageRefreshable>();
         _ = viewModel.DatabaseLabel.Should().Be(Strings.Msg_NoDatabaseLoaded);

         await viewModel.SubmitUsernameAsync(UnitTestsHelper.GetUsername());
         string fileName = Path.GetFileName(UnitTestsHelper.ComputeDatabaseFilePath());
         _ = viewModel.VaultFileName.Should().Be(fileName);
         _ = viewModel.DatabaseLabel.Should().Be(Strings.Format(nameof(Strings.Msg_DatabaseLabel), fileName));

         viewModel.Reset();
         _ = viewModel.VaultFileName.Should().BeEmpty();

         List<string?> changed = [];
         viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
         ((ILanguageRefreshable)viewModel).OnLanguageChanged();
         _ = changed.Should().Contain(nameof(LoginViewModel.DatabaseLabel));

         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A host that closes while the vault is opening, or during the last
       * passkey, gets neither a half-open vault nor a session: the vault is
       * closed and released.
      */
      public async Task Case14_DisposedDuringOpenOrLogin_ClosesTheVault()
      {
         string testName = nameof(Case14_DisposedDuringOpenOrLogin_ClosesTheVault);
         UnitTestsHelper.ClearTestEnvironment(testName);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase previous = UnitTestsHelper.CreateTestDatabase(passkeys, testName);
         _ = previous.User!.AddService("PendingService");
         previous.Close();
         FakeDialogService dialogs = _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory(testName));

         LoginViewModel opening = new();
         AppServices.VaultSources = new InterruptingVaultSourceFactory(opening.Dispose);
         await opening.SubmitUsernameAsync(testName);
         _ = opening.DatabaseOpened.Should().BeFalse();

         AppServices.VaultSources = new PathVaultSourceFactory();
         LoginViewModel loggingIn = new();
         int loggedIn = 0;
         loggingIn.LoggedIn += (_, _) => loggedIn++;
         await loggingIn.SubmitUsernameAsync(testName);
         _ = loggingIn.DatabaseOpened.Should().BeTrue("the first attempt released the vault");

         dialogs.Confirming = loggingIn.Dispose;
         bool result = false;
         foreach (string passkey in passkeys)
         {
            result = await loggingIn.SubmitPasskeyAsync(passkey);
         }

         _ = result.Should().BeFalse();
         _ = loggedIn.Should().Be(0);
         _ = loggingIn.IsLoggedIn.Should().BeFalse();
         _ = AppServices.Session.Database.Should().BeNull();

         dialogs.Confirming = null;
         using LoginViewModel again = new();
         await again.SubmitUsernameAsync(testName);
         _ = again.DatabaseOpened.Should().BeTrue("the disposed login closed the vault");

         again.Reset();
         UnitTestsHelper.ClearTestEnvironment(testName);
      }

      [TestMethod]
      /*
       * A reset requested while the vault is opening, or during the last
       * passkey, waits for Core to return, then closes the vault and clears
       * the credentials instead of racing the open or the login.
      */
      public async Task Case15_ResetDuringOpenOrLogin_RunsOnceTheCallReturns()
      {
         string testName = nameof(Case15_ResetDuringOpenOrLogin_RunsOnceTheCallReturns);
         UnitTestsHelper.ClearTestEnvironment(testName);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase previous = UnitTestsHelper.CreateTestDatabase(passkeys, testName);
         _ = previous.User!.AddService("PendingService");
         previous.Close();
         FakeDialogService dialogs = _configureHost(UnitTestsHelper.ComputeDatabaseFileDirectory(testName));

         using LoginViewModel opening = new();
         int resets = 0;
         opening.CredentialsReset += (_, _) => resets++;
         AppServices.VaultSources = new InterruptingVaultSourceFactory(opening.Reset);
         await opening.SubmitUsernameAsync(testName);
         _ = opening.DatabaseOpened.Should().BeFalse();
         _ = opening.IsBusy.Should().BeFalse();
         _ = resets.Should().Be(1);

         AppServices.VaultSources = new PathVaultSourceFactory();
         using LoginViewModel loggingIn = new();
         int loggedIn = 0;
         loggingIn.LoggedIn += (_, _) => loggedIn++;
         await loggingIn.SubmitUsernameAsync(testName);
         _ = loggingIn.DatabaseOpened.Should().BeTrue("the reset open released the vault");

         dialogs.Confirming = loggingIn.Reset;
         bool result = false;
         foreach (string passkey in passkeys)
         {
            result = await loggingIn.SubmitPasskeyAsync(passkey);
         }

         _ = result.Should().BeFalse();
         _ = loggedIn.Should().Be(0);
         _ = loggingIn.IsLoggedIn.Should().BeFalse();
         _ = loggingIn.DatabaseOpened.Should().BeFalse();
         _ = AppServices.Session.Database.Should().BeNull();

         dialogs.Confirming = null;
         using LoginViewModel again = new();
         await again.SubmitUsernameAsync(testName);
         _ = again.DatabaseOpened.Should().BeTrue("the reset login closed the vault");

         again.Reset();
         UnitTestsHelper.ClearTestEnvironment(testName);
      }

      private sealed class InterruptingVaultSourceFactory(Action interrupt) : IVaultSourceFactory
      {
         private readonly PathVaultSourceFactory _inner = new();

         public Task<IVaultSource> OpenPathAsync(string path, CancellationToken cancellationToken = default)
         {
            interrupt();
            return _inner.OpenPathAsync(path, cancellationToken);
         }

         public Task<IVaultSource> CreatePathAsync(string path, CancellationToken cancellationToken = default)
            => _inner.CreatePathAsync(path, cancellationToken);

         public Task<IVaultSource?> CreateInteractiveAsync(string suggestedFileName, CancellationToken cancellationToken = default)
            => _inner.CreateInteractiveAsync(suggestedFileName, cancellationToken);
      }

      private static FakeTimerFactory _configureIdleTimeout(int seconds)
      {
         FakeTimerFactory timers = new();
         AppServices.Timers = timers;
         AppServices.Settings = new JsonAppSettingsStore(AppServices.Paths!);
         AppServices.Settings.Current.LoginIdleTimeoutSeconds = seconds;
         return timers;
      }

      private static FakeDialogService _configureHost(string defaultDatabaseDirectory)
      {
         FakeDialogService dialogs = new();
         AppServices.Reset();
         AppServices.Ui = new InlineUiDispatcher();
         AppServices.Dialogs = dialogs;
         AppServices.Paths = new FakeAppPaths(defaultDatabaseDirectory);
         AppServices.VaultSources = new PathVaultSourceFactory();
         AppServices.Clipboard = new FakeClipboardService();
         AppServices.PasswordFactory = UnitTestsHelper.FastPasswordFactory;
         return dialogs;
      }
   }
}
