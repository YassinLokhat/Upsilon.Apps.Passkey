using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Fakes;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class WarningUnitTests
   {
      [TestMethod]
      /*
       * Accounts that share a password raise DuplicatedPasswordsWarning when at
       * least one of them opted in; accounts with unique passwords do not.
      */
      public void Case01_DuplicatedPasswordsWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = WarningType.DuplicatedPasswordsWarning;

         IService service = database.User.AddService("DupService");
         IAccount sharedA = service.AddAccount("A", ["a@test"], "shared-secret");
         IAccount sharedB = service.AddAccount("B", ["b@test"], "shared-secret");
         IAccount unique = service.AddAccount("C", ["c@test"], "unique-secret");
         sharedA.Options = AccountOption.WarnIfDuplicatedPassword;
         sharedB.Options = AccountOption.None;
         unique.Options = AccountOption.WarnIfDuplicatedPassword;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.DuplicatedPasswordsWarning, database.Save);

         IWarning duplicate = warnings.Single(w => w.WarningType == WarningType.DuplicatedPasswordsWarning);
         _ = duplicate.Accounts.Should().BeEquivalentTo([sharedA, sharedB]);
         _ = duplicate.Accounts.Should().NotContain(unique);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * An expired password history raises PasswordUpdateReminderWarning; a
       * freshly dated one does not.
      */
      public void Case02_PasswordUpdateReminderWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = WarningType.PasswordUpdateReminderWarning;

         IService service = database.User.AddService("ExpiryService");
         IAccount stale = service.AddAccount("Stale", ["stale@test"], "stale-password");
         IAccount fresh = service.AddAccount("Fresh", ["fresh@test"], "fresh-password");
         stale.Options = AccountOption.None;
         fresh.Options = AccountOption.None;
         stale.PasswordUpdateReminderDelay = 3;
         fresh.PasswordUpdateReminderDelay = 3;

         Account staleConcrete = (Account)stale;
         staleConcrete.Passwords.Clear();
         staleConcrete.Passwords[DateTime.Now.AddMonths(-6)] = ProtectedSecret.Protect("stale-password");

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.PasswordUpdateReminderWarning, database.Save);

         IWarning reminder = warnings.Single(w => w.WarningType == WarningType.PasswordUpdateReminderWarning);
         _ = reminder.Accounts.Should().Contain(stale);
         _ = reminder.Accounts.Should().NotContain(fresh);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A password the factory reports as leaked raises PasswordLeakedWarning
       * only for accounts that opted into leak checks, and stamps PasswordLeaked.
      */
      public void Case03_PasswordLeakedWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         FakePasswordFactory factory = new();
         factory.MarkLeaked("pwned-password");

         IDatabase database = Database.Create(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            factory,
            UnitTestsHelper.ClipboardManager,
            databaseFile,
            username,
            passkeys);

         database.User!.Settings.WarningsToNotify = WarningType.PasswordLeakedWarning;

         IService service = database.User.AddService("LeakService");
         IAccount watched = service.AddAccount("Watched", ["watched@test"], "pwned-password");
         IAccount ignored = service.AddAccount("Ignored", ["ignored@test"], "pwned-password");
         IAccount safe = service.AddAccount("Safe", ["safe@test"], "safe-password");
         watched.Options = AccountOption.WarnIfPasswordLeaked;
         ignored.Options = AccountOption.None;
         safe.Options = AccountOption.WarnIfPasswordLeaked;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.PasswordLeakedWarning, database.Save);

         IWarning leaked = warnings.Single(w => w.WarningType == WarningType.PasswordLeakedWarning);
         _ = leaked.Accounts.Should().Contain(watched);
         _ = leaked.Accounts.Should().NotContain(ignored);
         _ = leaked.Accounts.Should().NotContain(safe);
         _ = ((Account)watched).PasswordLeaked.Should().BeTrue();
         _ = ((Account)safe).PasswordLeaked.Should().BeFalse();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
