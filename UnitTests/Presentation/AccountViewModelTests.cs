using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class AccountViewModelTests
   {
      private const string TestUsername = nameof(AccountViewModelTests);

      private IDatabase? _database;

      [TestInitialize]
      public void Initialize()
      {
         ViewModelTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(2);
         _database = UnitTestsHelper.CreateTestDatabase(passkeys, TestUsername);
         AppServices.Session.StartSession(_database);
      }

      [TestCleanup]
      public void Cleanup()
      {
         _database = null;
         ViewModelTestServices.Reset();
         UnitTestsHelper.ClearTestEnvironment(TestUsername);
      }

      [TestMethod]
      /*
       * Editing Password through the VM must queue an alert scan so duplicate /
       * weak / leak menus update without waiting for Save.
      */
      public void Case01_PasswordEdit_RefreshesDuplicatedPasswordAlerts()
      {
         _database!.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.DuplicatedPasswords]);

         IService service = _database.User.AddService("DupSvc");
         IAccount first = service.AddAccount("A", UnitTestsHelper.Ids("a@test"), "unique-a");
         IAccount second = service.AddAccount("B", UnitTestsHelper.Ids("b@test"), "unique-b");
         first.Options = AccountOption.WarnIfDuplicatedPassword;
         second.Options = AccountOption.WarnIfDuplicatedPassword;
         _ = UnitTestsHelper.WaitForAlerts(_database, _database.Save);

         using AccountViewModel vm = new(first);

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(
            _database,
            AlertKinds.DuplicatedPasswords,
            () => vm.Password = "unique-b");

         IAccountsAlert duplicate = alerts.OfType<IAccountsAlert>()
            .Single(w => w.Kind == AlertKinds.DuplicatedPasswords);
         _ = duplicate.Accounts.Should().BeEquivalentTo([first, second]);
      }
   }
}
