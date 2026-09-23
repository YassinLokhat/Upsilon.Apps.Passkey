using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Core
{
   [TestClass]
   public sealed class UserUnitTests
   {
      [TestMethod]
      /*
       * Updating User creates an autosave file and don't update the save file.
      */
      public void Case01_UserUpdateWithoutSaving()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string oldDatabaseContent = UnitTestsHelper.ReadFileZipEntry(databaseFile, "database");
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         databaseLoaded.User.Username = newUsername;
         databaseLoaded.User.Passkeys = newPasskeys;
         databaseLoaded.User.Settings.LogoutTimeout = logoutTimeout;
         databaseLoaded.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;

         // Then
         databaseLoaded.User.HasChanged().Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Username)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Passkeys)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Settings.LogoutTimeout)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Settings.CleaningClipboardTimeout)).Should().BeTrue();

         // When
         databaseLoaded.Close();

         // Then
         _ = UnitTestsHelper.ReadFileZipEntry(databaseFile, "database").Should().Be(oldDatabaseContent);

         // Finally
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then Database.Save will save the update in the database file and delete the autosave file,
       * Then Database.Open loads correctly the updated database file.
      */
      public void Case02_UserUpdateThenSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string oldUsername = UnitTestsHelper.GetUsername();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase();
         string newUsername = "new_" + oldUsername;
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         Stack<ExpectedActivity> expectedActivities = new();
         Stack<ExpectedActivity> expectedLogAlerts = new();

         // When
         // User.ToString() resolves to Host.Username (open name), not the mutated User.Username.
         string userNameAtChange = databaseCreated.User.ToString();
         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Username = newUsername;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.Passkeys = newPasskeys;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.LogoutTimeout), fieldValue: $"{logoutTimeout}"));
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.CleaningClipboardTimeout), fieldValue: $"{cleaningClipboardTimeout}"));
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));
         databaseCreated.Save();
         expectedActivities.Push(ExpectedActivity.DatabaseSaved(databaseCreated.User.ToString()));
         databaseCreated.Close();
         expectedActivities.Push(ExpectedActivity.UserLoggedOut(newUsername));
         expectedActivities.Push(ExpectedActivity.DatabaseClosed(newUsername));
         IDatabase databaseLoaded = Database.Open(UnitTestsHelper.CryptographyCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.FastPasswordFactory,
            UnitTestsHelper.ClipboardManager,
            UnitTestsHelper.SecretMemoryProtector,
            databaseFile,
            newUsername);
         expectedActivities.Push(ExpectedActivity.DatabaseOpened(newUsername));
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedActivities.Push(ExpectedActivity.UserLoggedIn(databaseLoaded.User.ToString()));

         // Then
         _ = databaseLoaded.User.Should().NotBeNull();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Settings.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.Settings.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityAlertsShouldMatch(databaseLoaded, [.. expectedLogAlerts]);

         _ = UnitTestsHelper.FlattenCoreAlerts(databaseLoaded).Should().NotBeEmpty();

         // Finally
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then Database.Open loads the database file without the updated data,
       * Then HandleAutoSave updates the database object and the database file,
       * Then Database.Open loads correctly the updated database file.
      */
      public void Case03_UserUpdateButNotSaved_CaseMergeAndSave()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string oldUsername = UnitTestsHelper.GetUsername();
         string[] oldPasskeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(oldPasskeys);
         string newUsername = "new_" + oldUsername;
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         Stack<ExpectedActivity> expectedActivities = new();
         Stack<ExpectedActivity> expectedLogAlerts = new();

         // When
         string userNameAtChange = databaseCreated.User.ToString();
         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Username = newUsername;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.Passkeys = newPasskeys;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.LogoutTimeout), fieldValue: $"{logoutTimeout}"));
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.CleaningClipboardTimeout), fieldValue: $"{cleaningClipboardTimeout}"));
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));

         databaseCreated.Close();
         expectedActivities.Push(ExpectedActivity.UserLoggedOut(oldUsername, withoutSaving: true));
         expectedLogAlerts.Push(ExpectedActivity.UserLoggedOut(oldUsername, withoutSaving: true));
         expectedActivities.Push(ExpectedActivity.DatabaseClosed(oldUsername));
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, out IAlert[] alerts, AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile);
         expectedActivities.Push(ExpectedActivity.DatabaseOpened(oldUsername));
         expectedActivities.Push(ExpectedActivity.UserLoggedIn(oldUsername));
         expectedActivities.Push(ExpectedActivity.AutosaveMerged(databaseLoaded.User.ToString(), ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile));
         expectedLogAlerts.Push(ExpectedActivity.AutosaveMerged(databaseLoaded.User.ToString(), ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile));

         // Then
         _ = databaseLoaded.User.HasChanged().Should().BeFalse();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.Settings.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.Settings.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         _ = alerts.Should().NotContain(w =>
            w.Kind == AlertKinds.DuplicatedPasswords || w.Kind == AlertKinds.PasswordUpdateReminder);

         // When
         databaseLoaded.Close();
         expectedActivities.Push(ExpectedActivity.UserLoggedOut(newUsername));
         expectedActivities.Push(ExpectedActivity.DatabaseClosed(newUsername));

         databaseLoaded = Database.Open(UnitTestsHelper.CryptographyCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.FastPasswordFactory,
            UnitTestsHelper.ClipboardManager,
            UnitTestsHelper.SecretMemoryProtector,
            databaseFile,
            newUsername);
         expectedActivities.Push(ExpectedActivity.DatabaseOpened(newUsername));
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedActivities.Push(ExpectedActivity.UserLoggedIn(databaseLoaded.User.ToString()));

         // Then
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.Settings.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.Settings.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityAlertsShouldMatch(databaseLoaded, [.. expectedLogAlerts]);

         _ = UnitTestsHelper.FlattenCoreAlerts(databaseLoaded).Should().NotBeEmpty();

         // Finally
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then Database.Open loads the database file without the updated data,
       * Then HandleAutoSave updates the database object but not save the database file,
       * Then Database.Open loads correctly the updated database file but tag the data "unsaved".
      */
      public void Case04_UserUpdateButNotSaved_CaseMergeWithoutSaving()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string oldUsername = UnitTestsHelper.GetUsername();
         string[] oldPasskeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(oldPasskeys);
         string newUsername = "new_" + oldUsername;
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         Stack<ExpectedActivity> expectedActivities = new();
         Stack<ExpectedActivity> expectedLogAlerts = new();

         // When
         string userNameAtChange = databaseCreated.User.ToString();
         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Username = newUsername;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: userNameAtChange, fieldName: nameof(databaseCreated.User.Username), fieldValue: newUsername));
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.Passkeys = newPasskeys;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Passkeys), fieldValue: string.Empty));
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         databaseCreated.User.Settings.LogoutTimeout = logoutTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.LogoutTimeout), fieldValue: $"{logoutTimeout}"));
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.User.Settings.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(false, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.CleaningClipboardTimeout), fieldValue: $"{cleaningClipboardTimeout}"));
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         databaseCreated.User.Settings.AlertsToNotify = _dupAndReminderNotify;
         expectedActivities.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));
         expectedLogAlerts.Push(ExpectedActivity.ItemUpdated(true, username: databaseCreated.User.ToString(), fieldName: nameof(databaseCreated.User.Settings.AlertsToNotify), fieldValue: _dupAndReminderNotify.ToString()));

         databaseCreated.Close();
         expectedActivities.Push(ExpectedActivity.UserLoggedOut(oldUsername, withoutSaving: true));
         expectedLogAlerts.Push(ExpectedActivity.UserLoggedOut(oldUsername, withoutSaving: true));
         expectedActivities.Push(ExpectedActivity.DatabaseClosed(oldUsername));
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, out IAlert[] alerts, AutoSaveMergeBehavior.MergeWithoutSavingAndKeepAutoSaveFile);
         expectedActivities.Push(ExpectedActivity.DatabaseOpened(oldUsername));
         expectedActivities.Push(ExpectedActivity.UserLoggedIn(oldUsername));
         expectedActivities.Push(ExpectedActivity.AutosaveMerged(databaseLoaded.User.ToString(), ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile));
         expectedLogAlerts.Push(ExpectedActivity.AutosaveMerged(databaseLoaded.User.ToString(), ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile));

         // Then
         _ = databaseLoaded.User.HasChanged().Should().BeTrue();
         _ = databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Username)).Should().BeTrue();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Passkeys)).Should().BeTrue();
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Settings.LogoutTimeout)).Should().BeTrue();
         _ = databaseLoaded.User.Settings.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Settings.CleaningClipboardTimeout)).Should().BeTrue();
         _ = databaseLoaded.User.Settings.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         _ = alerts.Should().NotContain(w =>
            w.Kind == AlertKinds.DuplicatedPasswords || w.Kind == AlertKinds.PasswordUpdateReminder);

         // When
         databaseLoaded.Save();
         expectedActivities.Push(ExpectedActivity.DatabaseSaved(databaseLoaded.User.ToString()));
         databaseLoaded.Close();
         expectedActivities.Push(ExpectedActivity.UserLoggedOut(newUsername));
         expectedActivities.Push(ExpectedActivity.DatabaseClosed(newUsername));

         databaseLoaded = Database.Open(UnitTestsHelper.CryptographyCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.FastPasswordFactory,
            UnitTestsHelper.ClipboardManager,
            UnitTestsHelper.SecretMemoryProtector,
            databaseFile,
            newUsername);
         expectedActivities.Push(ExpectedActivity.DatabaseOpened(newUsername));
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedActivities.Push(ExpectedActivity.UserLoggedIn(databaseLoaded.User.ToString()));

         // Then
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.Settings.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.Settings.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityAlertsShouldMatch(databaseLoaded, [.. expectedLogAlerts]);

         _ = UnitTestsHelper.FlattenCoreAlerts(databaseLoaded).Should().NotBeEmpty();

         // Finally
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * DontMergeAndKeepAutoSaveFile leaves the vault unchanged and keeps the
       * recovery file so a later merge can still apply the edits.
      */
      public void Case05_UserUpdateButNotSaved_CaseDontMergeAndKeep()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string oldUsername = UnitTestsHelper.GetUsername();
         string[] oldPasskeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(oldPasskeys);
         string newUsername = "new_" + oldUsername;

         databaseCreated.User.Username = newUsername;
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, out _, AutoSaveMergeBehavior.DontMergeAndKeepAutoSaveFile);

         _ = databaseLoaded.User.Username.Should().Be(oldUsername);
         _ = databaseLoaded.User.HasChanged().Should().BeTrue("the recovery file is kept, so pending edits stay dirty");

         databaseLoaded.Close();

         IDatabase merged = UnitTestsHelper.OpenTestDatabase(oldPasskeys, out _, AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile);
         _ = merged.User.Username.Should().Be(newUsername);

         merged.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Assigning an ISettings that is not the Core implementation is rejected.
      */
      public void Case06_SettingsSetter_RejectsUnknownImplementation()
      {
         UnitTestsHelper.ClearTestEnvironment();
         IDatabase database = UnitTestsHelper.CreateTestDatabase();

         Action assign = () => database.User!.Settings = new ForeignSettings();
         assign.Should().Throw<InvalidCastException>();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      private static readonly AlertKindList _dupAndReminderNotify = new(
      [
         AlertKinds.DuplicatedPasswords,
         AlertKinds.PasswordUpdateReminder,
      ]);

      private sealed class ForeignSettings : ISettings
      {
         public int LogoutTimeout { get; set; }
         public int CleaningClipboardTimeout { get; set; }
         public int ShowPasswordDelay { get; set; }
         public int NumberOfOldPasswordToKeep { get; set; }
         public int NumberOfMonthActivitiesToKeep { get; set; }
         public AlertKindList AlertsToNotify { get; set; } = new([]);
         public string Language { get; set; } = string.Empty;
         public string Theme { get; set; } = string.Empty;
      }
   }
}
