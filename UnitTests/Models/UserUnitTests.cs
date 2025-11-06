using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;
using Upsilon.Apps.Passkey.Core.Public.Utils;

namespace Upsilon.Apps.PassKey.UnitTests.Models
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
         string oldDatabaseContent = File.ReadAllText(UnitTestsHelper.ComputeDatabaseFilePath());
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         databaseLoaded.User.Username = newUsername;
         databaseLoaded.User.Passkeys = newPasskeys;
         databaseLoaded.User.LogoutTimeout = logoutTimeout;
         databaseLoaded.User.CleaningClipboardTimeout = cleaningClipboardTimeout;

         // Then
         databaseLoaded.User.HasChanged().Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Username)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.Passkeys)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.LogoutTimeout)).Should().BeTrue();
         databaseLoaded.User.HasChanged(nameof(databaseLoaded.User.CleaningClipboardTimeout)).Should().BeTrue();

         // When
         databaseLoaded.Close();

         // Then
         _ = File.Exists(UnitTestsHelper.ComputeAutoSaveFilePath()).Should().BeTrue();
         _ = File.ReadAllText(UnitTestsHelper.ComputeDatabaseFilePath()).Should().Be(oldDatabaseContent);

         // Finaly
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
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string oldUsername = UnitTestsHelper.GetUsername();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase();
         string newUsername = "new_" + oldUsername;
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         Stack<string> expectedLogs = new();
         Stack<string> expectedLogWarnings = new();

         // When
         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Username = newUsername;
         expectedLogs.Push($"Warning : User {oldUsername}'s username has been set to {newUsername}");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s username has been set to {newUsername}");
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.Passkeys = newPasskeys;
         expectedLogs.Push($"Warning : User {oldUsername}'s passkeys has been updated");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s passkeys has been updated");
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         expectedLogs.Push($"Information : User {oldUsername}'s logout timeout has been set to {logoutTimeout}");
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedLogs.Push($"Information : User {oldUsername}'s cleaning clipboard timeout has been set to {cleaningClipboardTimeout}");
         databaseCreated.User.WarningsToNotify = WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning;
         databaseCreated.User.WarningsToNotify = WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning;
         expectedLogs.Push($"Warning : User {oldUsername}'s warnings to notify has been set to {WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning}");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s warnings to notify has been set to {WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning}");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         databaseCreated.Save();
         expectedLogs.Push($"Information : User {newUsername}'s database saved");
         databaseCreated.Close();
         expectedLogs.Push($"Information : User {newUsername} logged out");
         expectedLogs.Push($"Information : User {newUsername}'s database closed");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();

         // When
         IDatabase databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            newUsername);
         expectedLogs.Push($"Information : User {newUsername}'s database opened");
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedLogs.Push($"Information : User {newUsername} logged in");

         // Then
         _ = databaseLoaded.User.Should().NotBeNull();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         _ = File.Exists(autoSaveFile).Should().BeFalse();

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         _ = databaseLoaded.Warnings.Should().NotBeEmpty();

         // Finaly
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
      public void Case03_UserUpdateButNotSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string oldUsername = UnitTestsHelper.GetUsername();
         string[] oldPasskeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(oldPasskeys);
         string newUsername = "new_" + oldUsername;
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         Stack<string> expectedLogs = new();
         Stack<string> expectedLogWarnings = new();

         // When
         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Username = newUsername;
         expectedLogs.Push($"Warning : User {oldUsername}'s username has been set to {newUsername}");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s username has been set to {newUsername}");
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.Passkeys = newPasskeys;
         expectedLogs.Push($"Warning : User {oldUsername}'s passkeys has been updated");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s passkeys has been updated");
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         expectedLogs.Push($"Information : User {oldUsername}'s logout timeout has been set to {logoutTimeout}");
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedLogs.Push($"Information : User {oldUsername}'s cleaning clipboard timeout has been set to {cleaningClipboardTimeout}");
         databaseCreated.User.WarningsToNotify = WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning;
         databaseCreated.User.WarningsToNotify = WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning;
         expectedLogs.Push($"Warning : User {oldUsername}'s warnings to notify has been set to {WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning}");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s warnings to notify has been set to {WarningType.DuplicatedPasswordsWarning | WarningType.PasswordUpdateReminderWarning}");

         databaseCreated.Close();
         expectedLogs.Push($"Warning : User {oldUsername} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {oldUsername} logged out without saving");
         expectedLogs.Push($"Information : User {oldUsername}'s database closed");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, out IWarning[] warnings, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"Information : User {oldUsername}'s database opened");
         expectedLogs.Push($"Information : User {oldUsername} logged in");
         expectedLogs.Push($"Warning : User {oldUsername}'s autosave merged");
         expectedLogWarnings.Push($"Warning : User {oldUsername}'s autosave merged");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         _ = warnings.Should().BeEmpty();

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"Information : User {newUsername} logged out");
         expectedLogs.Push($"Information : User {newUsername}'s database closed");

         databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            newUsername);
         expectedLogs.Push($"Information : User {newUsername}'s database opened");
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedLogs.Push($"Information : User {newUsername} logged in");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         _ = databaseLoaded.Warnings.Should().NotBeEmpty();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
