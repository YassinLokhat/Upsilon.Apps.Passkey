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
   public sealed class AlertUnitTests
   {
      // VaultSecuritySettings also reports when no accounts enable duplicate /
      // reminder / leaked monitoring; tests that assert a clean posture must
      // keep those kinds enabled alongside VaultSecuritySettings.
      private static readonly AlertKindList SecurityPostureNotify = new(
      [
         AlertKinds.VaultSecuritySettings,
         AlertKinds.DuplicatedPasswords,
         AlertKinds.PasswordUpdateReminder,
         AlertKinds.PasswordLeaked,
      ]);

      [TestMethod]
      /*
       * Accounts that share a password raise DuplicatedPasswords when at
       * least one of them opted in; accounts with unique passwords do not.
      */
      public void Case01_DuplicatedPasswordsAlert()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.DuplicatedPasswords]);

         IService service = database.User.AddService("DupService");
         IAccount sharedA = service.AddAccount("A", UnitTestsHelper.Ids("a@test"), "shared-secret");
         IAccount sharedB = service.AddAccount("B", UnitTestsHelper.Ids("b@test"), "shared-secret");
         IAccount unique = service.AddAccount("C", UnitTestsHelper.Ids("c@test"), "unique-secret");
         sharedA.Options = AccountOption.WarnIfDuplicatedPassword;
         sharedB.Options = AccountOption.None;
         unique.Options = AccountOption.WarnIfDuplicatedPassword;

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.DuplicatedPasswords, database.Save);

         IAccountsAlert duplicate = alerts.OfType<IAccountsAlert>()
            .Single(w => w.Kind == AlertKinds.DuplicatedPasswords);
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
      public void Case02_PasswordUpdateReminderAlert()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.PasswordUpdateReminder]);

         IService service = database.User.AddService("ExpiryService");
         IAccount stale = service.AddAccount("Stale", UnitTestsHelper.Ids("stale@test"), "stale-password");
         IAccount fresh = service.AddAccount("Fresh", UnitTestsHelper.Ids("fresh@test"), "fresh-password");
         stale.Options = AccountOption.None;
         fresh.Options = AccountOption.None;
         stale.PasswordUpdateReminderDelay = 3;
         fresh.PasswordUpdateReminderDelay = 3;

         Account staleConcrete = (Account)stale;
         staleConcrete.Passwords.Clear();
         staleConcrete.Passwords[DateTime.Now.AddMonths(-6)] = ProtectedSecret.Protect("stale-password");

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.PasswordUpdateReminder, database.Save);

         IAccountsAlert reminder = alerts.OfType<IAccountsAlert>()
            .Single(w => w.Kind == AlertKinds.PasswordUpdateReminder);
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
      public void Case03_PasswordLeakedAlert()
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

         database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.PasswordLeaked]);

         IService service = database.User.AddService("LeakService");
         IAccount watched = service.AddAccount("Watched", UnitTestsHelper.Ids("watched@test"), "pwned-password");
         IAccount ignored = service.AddAccount("Ignored", UnitTestsHelper.Ids("ignored@test"), "pwned-password");
         IAccount safe = service.AddAccount("Safe", UnitTestsHelper.Ids("safe@test"), "safe-password");
         watched.Options = AccountOption.WarnIfPasswordLeaked;
         ignored.Options = AccountOption.None;
         safe.Options = AccountOption.WarnIfPasswordLeaked;

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.PasswordLeaked, database.Save);

         IAccountsAlert leaked = alerts.OfType<IAccountsAlert>()
            .Single(w => w.Kind == AlertKinds.PasswordLeaked);
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
         database.User!.Settings.AlertsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 0;
         database.User.Settings.CleaningClipboardTimeout = 0;
         database.User.Settings.ShowPasswordDelay = 0;

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.VaultSecuritySettings, database.Save);

         IVaultSecuritySettingsAlert posture = alerts.OfType<IVaultSecuritySettingsAlert>().Single();
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.AutoLogoutDisabled);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.ClipboardCleaningDisabled);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.QrAutoCloseDisabled);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;

         IAlert[] cleared = UnitTestsHelper.WaitForAlerts(database, database.Save);
         _ = cleared.Should().NotContain(w => w.Kind == AlertKinds.VaultSecuritySettings);

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
         database.User!.Settings.AlertsToNotify = SecurityPostureNotify;
         database.User.Settings.LogoutTimeout = 5;
         database.User.Settings.CleaningClipboardTimeout = 30;
         database.User.Settings.ShowPasswordDelay = 5000;

         IService service = database.User.AddService("Unmonitored");
         IAccount account = service.AddAccount("A", UnitTestsHelper.Ids("a@test"), "secret");
         account.Options = AccountOption.None;
         account.PasswordUpdateReminderDelay = 0;

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.VaultSecuritySettings, database.Save);

         IVaultSecuritySettingsAlert posture = alerts.OfType<IVaultSecuritySettingsAlert>().Single();
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = posture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // Two accounts with leak, still zero duplicate / reminder → warn only those two.
         account.Options = AccountOption.WarnIfPasswordLeaked;
         IAccount alsoLeaked = service.AddAccount("B", UnitTestsHelper.Ids("b@test"), "other-secret");
         alsoLeaked.Options = AccountOption.WarnIfPasswordLeaked;
         alsoLeaked.PasswordUpdateReminderDelay = 0;

         IAlert[] afterLeak = UnitTestsHelper.WaitForAlerts(database, database.Save);
         IVaultSecuritySettingsAlert still = afterLeak.OfType<IVaultSecuritySettingsAlert>().Single();
         _ = still.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = still.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = still.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         // A third account without leak does not revive NoAccountLeakCheck.
         IAccount uncovered = service.AddAccount("C", UnitTestsHelper.Ids("c@test"), "third-secret");
         uncovered.Options = AccountOption.None;
         IAlert[] mixed = UnitTestsHelper.WaitForAlerts(database, database.Save);
         IVaultSecuritySettingsAlert mixedPosture = mixed.OfType<IVaultSecuritySettingsAlert>().Single();
         _ = mixedPosture.Issues.Should().NotHaveFlag(SecuritySettingsIssue.NoAccountLeakCheck);
         _ = mixedPosture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountDuplicateCheck);
         _ = mixedPosture.Issues.Should().HaveFlag(SecuritySettingsIssue.NoAccountUpdateReminder);

         account.Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;
         account.PasswordUpdateReminderDelay = 6;

         IAlert[] cleared = UnitTestsHelper.WaitForAlerts(database, database.Save);
         _ = cleared.Should().NotContain(w => w.Kind == AlertKinds.VaultSecuritySettings);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Fewer than RecommendedPasskeyCount onion layers raises InsufficientPasskeys.
      */
      public void Case06_InsufficientPasskeysAlert()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(1);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.InsufficientPasskeys]);

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.InsufficientPasskeys, database.Save);

         IInsufficientPasskeysAlert insufficient = alerts.OfType<IInsufficientPasskeysAlert>().Single();
         _ = insufficient.Count.Should().Be(1);
         _ = insufficient.RecommendedMinimum.Should().Be(AlertKinds.RecommendedPasskeyCount);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A short passkey layer raises WeakPasskey after the onion is updated.
      */
      public void Case07_WeakPasskeyAlert()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = ["StrongPasskeyOne!", "StrongPasskeyTwo!"];
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.WeakPasskey]);
         database.User.Passkeys = ["StrongPasskeyOne!", "short"];

         IAlert[] alerts = UnitTestsHelper.WaitForAlertKind(database, AlertKinds.WeakPasskey, database.Save);

         IWeakPasskeyAlert weak = alerts.OfType<IWeakPasskeyAlert>().Single();
         _ = weak.PasskeyIndexes.Should().Contain(1);
         _ = weak.Issues.Should().HaveFlag(SecretQualityIssue.TooShort);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * ActivityReview severity is the max of per-event severities: import /
       * ItemAdded / autosave merge are Info; export is Critical.
      */
      public void Case08_ActivityReviewSeverity_PerEventMax()
      {
         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.ImportingDataStarted)
            .Should().Be(AlertSeverity.Info);
         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.ItemAdded)
            .Should().Be(AlertSeverity.Info);
         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile)
            .Should().Be(AlertSeverity.Info);

         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.ItemUpdated)
            .Should().Be(AlertSeverity.Warning);
         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.ItemDeleted)
            .Should().Be(AlertSeverity.Warning);

         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.ExportingDataStarted)
            .Should().Be(AlertSeverity.Critical);
         _ = ActivityReviewAlert.SeverityFor(ActivityEventType.LoginFailed)
            .Should().Be(AlertSeverity.Critical);

         Activity infoOnly = new(1, "id", "u", null, null, null, null, null,
            ActivityEventType.ImportingDataSucceded, needsReview: true);
         Activity export = new(2, "id", "u", null, null, null, null, null,
            ActivityEventType.ExportingDataSucceded, needsReview: true);

         _ = new ActivityReviewAlert([infoOnly]).Severity.Should().Be(AlertSeverity.Info);
         _ = new ActivityReviewAlert([infoOnly, export]).Severity.Should().Be(AlertSeverity.Critical);
      }
   }
}
