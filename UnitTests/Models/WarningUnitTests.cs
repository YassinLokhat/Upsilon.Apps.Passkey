using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Fakes;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class WarningUnitTests
   {
      // VaultSecuritySettings also reports when no accounts enable duplicate /
      // reminder / leaked monitoring; tests that assert a clean posture must
      // keep those kinds enabled alongside VaultSecuritySettings.
      private static readonly WarningKindList SecurityPostureNotify = new(
      [
         WarningKinds.VaultSecuritySettings,
         WarningKinds.DuplicatedPasswords,
         WarningKinds.PasswordUpdateReminder,
         WarningKinds.PasswordLeaked,
      ]);

      [TestMethod]
      /*
       * Accounts that share a password raise DuplicatedPasswords when at
       * least one of them opted in; accounts with unique passwords do not.
      */
      public void Case01_DuplicatedPasswordsWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = new WarningKindList([WarningKinds.DuplicatedPasswords]);

         IService service = database.User.AddService("DupService");
         IAccount sharedA = service.AddAccount("A", ["a@test"], "shared-secret");
         IAccount sharedB = service.AddAccount("B", ["b@test"], "shared-secret");
         IAccount unique = service.AddAccount("C", ["c@test"], "unique-secret");
         sharedA.Options = AccountOption.WarnIfDuplicatedPassword;
         sharedB.Options = AccountOption.None;
         unique.Options = AccountOption.WarnIfDuplicatedPassword;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.DuplicatedPasswords, database.Save);

         IAccountsWarning duplicate = warnings.OfType<IAccountsWarning>()
            .Single(w => w.Kind == WarningKinds.DuplicatedPasswords);
         _ = duplicate.Accounts.Should().BeEquivalentTo([sharedA, sharedB]);
         _ = duplicate.Accounts.Should().NotContain(unique);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * An expired password history raises PasswordUpdateReminder; a
       * freshly dated one does not.
      */
      public void Case02_PasswordUpdateReminderWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = new WarningKindList([WarningKinds.PasswordUpdateReminder]);

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

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.PasswordUpdateReminder, database.Save);

         IAccountsWarning reminder = warnings.OfType<IAccountsWarning>()
            .Single(w => w.Kind == WarningKinds.PasswordUpdateReminder);
         _ = reminder.Accounts.Should().Contain(stale);
         _ = reminder.Accounts.Should().NotContain(fresh);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A password the factory reports as leaked raises PasswordLeaked
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
            UnitTestsHelper.SecretMemoryProtector,
            databaseFile,
            username,
            passkeys);

         database.User!.Settings.WarningsToNotify = new WarningKindList([WarningKinds.PasswordLeaked]);

         IService service = database.User.AddService("LeakService");
         IAccount watched = service.AddAccount("Watched", ["watched@test"], "pwned-password");
         IAccount ignored = service.AddAccount("Ignored", ["ignored@test"], "pwned-password");
         IAccount safe = service.AddAccount("Safe", ["safe@test"], "safe-password");
         watched.Options = AccountOption.WarnIfPasswordLeaked;
         ignored.Options = AccountOption.None;
         safe.Options = AccountOption.WarnIfPasswordLeaked;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.PasswordLeaked, database.Save);

         IAccountsWarning leaked = warnings.OfType<IAccountsWarning>()
            .Single(w => w.Kind == WarningKinds.PasswordLeaked);
         _ = leaked.Accounts.Should().Contain(watched);
         _ = leaked.Accounts.Should().NotContain(ignored);
         _ = leaked.Accounts.Should().NotContain(safe);
         _ = ((Account)watched).PasswordLeaked.Should().BeTrue();
         _ = ((Account)safe).PasswordLeaked.Should().BeFalse();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Zeroed protective timers raise VaultSecuritySettings with the matching
       * issue flags; enabling the timers clears those issues.
      */
      public void Case04_VaultSecuritySettings_DisabledTimers()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 0;
         database.User.Settings.CleaningClipboardTimeout = 0;
         database.User.Settings.ShowPasswordDelay = 0;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.VaultSecuritySettings, database.Save);

         IVaultSecuritySettingsWarning posture = warnings.OfType<IVaultSecuritySettingsWarning>().Single();
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.AutoLogoutDisabled);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.ClipboardCleaningDisabled);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.QrAutoCloseDisabled);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;

         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.Save);
         _ = cleared.Should().NotContain(w => w.Kind == WarningKinds.VaultSecuritySettings);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Per monitoring kind: warn only when zero accounts opted in. One account
       * with leak checks clears NoAccountLeakCheck even if another account
       * leaves leak checks off; duplicate / reminder stay warned until some
       * account enables them.
      */
      public void Case05_VaultSecuritySettings_PerAccountMonitoringGaps()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;

         IService service = database.User.AddService("Unmonitored");
         IAccount account = service.AddAccount("A", ["a@test"], "secret");
         account.Options = AccountOption.None;
         account.PasswordUpdateReminderDelay = 0;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.VaultSecuritySettings, database.Save);

         IVaultSecuritySettingsWarning posture = warnings.OfType<IVaultSecuritySettingsWarning>().Single();
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // Two accounts with leak, still zero duplicate / reminder → warn only those two.
         account.Options = AccountOption.WarnIfPasswordLeaked;
         IAccount alsoLeaked = service.AddAccount("B", ["b@test"], "other-secret");
         alsoLeaked.Options = AccountOption.WarnIfPasswordLeaked;
         alsoLeaked.PasswordUpdateReminderDelay = 0;

         IWarning[] afterLeak = UnitTestsHelper.WaitForWarnings(database, database.Save);
         IVaultSecuritySettingsWarning still = afterLeak.OfType<IVaultSecuritySettingsWarning>().Single();
         _ = still.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = still.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = still.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // A third account without leak does not revive NoAccountLeakCheck.
         IAccount uncovered = service.AddAccount("C", ["c@test"], "third-secret");
         uncovered.Options = AccountOption.None;
         IWarning[] mixed = UnitTestsHelper.WaitForWarnings(database, database.Save);
         IVaultSecuritySettingsWarning mixedPosture = mixed.OfType<IVaultSecuritySettingsWarning>().Single();
         _ = mixedPosture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = mixedPosture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = mixedPosture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         account.Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;
         account.PasswordUpdateReminderDelay = 6;

         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.Save);
         _ = cleared.Should().NotContain(w => w.Kind == WarningKinds.VaultSecuritySettings);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Fewer than RecommendedPasskeyCount onion layers raises InsufficientPasskeys.
      */
      public void Case06_InsufficientPasskeysWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(1);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = new WarningKindList([WarningKinds.InsufficientPasskeys]);

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.InsufficientPasskeys, database.Save);

         IInsufficientPasskeysWarning insufficient = warnings.OfType<IInsufficientPasskeysWarning>().Single();
         _ = insufficient.Count.Should().Be(1);
         _ = insufficient.RecommendedMinimum.Should().Be(WarningKinds.RecommendedPasskeyCount);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A short passkey layer raises WeakPasskey after the onion is updated.
      */
      public void Case07_WeakPasskeyWarning()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = ["StrongPasskeyOne!", "StrongPasskeyTwo!"];
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = new WarningKindList([WarningKinds.WeakPasskey]);
         database.User.Passkeys = ["StrongPasskeyOne!", "short"];

         IWarning[] warnings = UnitTestsHelper.WaitForWarningKind(database, WarningKinds.WeakPasskey, database.Save);

         IWeakPasskeyWarning weak = warnings.OfType<IWeakPasskeyWarning>().Single();
         _ = weak.PasskeyIndexes.Should().Contain(1);
         _ = weak.Issues.Should().HaveFlag(SecretQualityIssue.TooShort);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
