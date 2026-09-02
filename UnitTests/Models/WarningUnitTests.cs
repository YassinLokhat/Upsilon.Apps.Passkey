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
      // SecuritySettingsWarning also reports when duplicate / reminder / leaked
      // notifications are off in WarningsToNotify; tests that assert a clean
      // posture must keep those flags enabled alongside SecuritySettingsWarning.
      private const WarningType SecurityPostureNotify
         = WarningType.SecuritySettingsWarning
         | WarningType.DuplicatedPasswordsWarning
         | WarningType.PasswordUpdateReminderWarning
         | WarningType.PasswordLeakedWarning;

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

      [TestMethod]
      /*
       * Zeroed protective timers raise SecuritySettingsWarning with the matching
       * issue flags; enabling the timers clears those issues.
      */
      public void Case04_SecuritySettingsWarning_DisabledTimers()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 0;
         database.User.Settings.CleaningClipboardTimeout = 0;
         database.User.Settings.ShowPasswordDelay = 0;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.SecuritySettingsWarning, database.Save);

         IWarning posture = warnings.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.AutoLogoutDisabled);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.ClipboardCleaningDisabled);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.QrAutoCloseDisabled);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.DuplicatePasswordNotificationsDisabled);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.PasswordUpdateReminderNotificationsDisabled);
         _ = posture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.PasswordLeakedNotificationsDisabled);

         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;

         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.Save);
         _ = cleared.Should().NotContain(w => w.WarningType == WarningType.SecuritySettingsWarning);

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
      public void Case05_SecuritySettingsWarning_PerAccountMonitoringGaps()
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

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.SecuritySettingsWarning, database.Save);

         IWarning posture = warnings.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // Two accounts with leak, still zero duplicate / reminder → warn only those two.
         account.Options = AccountOption.WarnIfPasswordLeaked;
         IAccount alsoLeaked = service.AddAccount("B", ["b@test"], "other-secret");
         alsoLeaked.Options = AccountOption.WarnIfPasswordLeaked;
         alsoLeaked.PasswordUpdateReminderDelay = 0;

         IWarning[] afterLeak = UnitTestsHelper.WaitForWarnings(database, database.Save);
         IWarning still = afterLeak.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = still.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = still.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = still.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // A third account without leak does not revive NoAccountLeakCheck.
         IAccount uncovered = service.AddAccount("C", ["c@test"], "third-secret");
         uncovered.Options = AccountOption.None;
         IWarning[] mixed = UnitTestsHelper.WaitForWarnings(database, database.Save);
         IWarning mixedPosture = mixed.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = mixedPosture.SecuritySettingsIssues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = mixedPosture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = mixedPosture.SecuritySettingsIssues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         account.Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;
         account.PasswordUpdateReminderDelay = 6;

         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.Save);
         _ = cleared.Should().NotContain(w => w.WarningType == WarningType.SecuritySettingsWarning);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * HostSecuritySettingsIssues contributes app-level idle-login and offline
       * filter flags into the same SecuritySettingsWarning bucket.
      */
      public void Case06_SecuritySettingsWarning_HostIssues()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.WarningsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;
         database.HostSecuritySettingsIssues = static () =>
            SecuritySettingsIssue.IdleLoginDisabled | SecuritySettingsIssue.OfflineLeakFilterUnavailable;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.SecuritySettingsWarning, database.Save);

         IWarning posture = warnings.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = posture.SecuritySettingsIssues.Should().Be(
            SecuritySettingsIssue.IdleLoginDisabled | SecuritySettingsIssue.OfflineLeakFilterUnavailable);

         database.HostSecuritySettingsIssues = null;
         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.RefreshWarnings);
         _ = cleared.Should().NotContain(w => w.WarningType == WarningType.SecuritySettingsWarning);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Clearing Notify Duplicated Passwords / Notify Password Update Reminder /
       * Notify Password Leaked in User Settings raises SecuritySettingsWarning
       * while Security Settings notifications remain enabled. Leaving Activity
       * Review off is intentional and does not contribute an issue flag.
       */
      public void Case07_SecuritySettingsWarning_NotifyFlagsDisabled()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;
         database.User.Settings.WarningsToNotify
            = WarningType.SecuritySettingsWarning
            | WarningType.ActivityReviewWarning;

         IWarning[] warnings = UnitTestsHelper.WaitForWarningType(database, WarningType.SecuritySettingsWarning, database.Save);

         IWarning posture = warnings.Single(w => w.WarningType == WarningType.SecuritySettingsWarning);
         _ = posture.SecuritySettingsIssues.Should().Be(
            SecuritySettingsIssue.DuplicatePasswordNotificationsDisabled
            | SecuritySettingsIssue.PasswordUpdateReminderNotificationsDisabled
            | SecuritySettingsIssue.PasswordLeakedNotificationsDisabled);

         database.User.Settings.WarningsToNotify = SecurityPostureNotify;

         IWarning[] cleared = UnitTestsHelper.WaitForWarnings(database, database.Save);
         _ = cleared.Should().NotContain(w => w.WarningType == WarningType.SecuritySettingsWarning);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
