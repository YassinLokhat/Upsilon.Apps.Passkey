using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
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
