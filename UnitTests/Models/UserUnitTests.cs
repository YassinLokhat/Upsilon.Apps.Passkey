using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;

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
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);

         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomStringArray();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         databaseLoaded.User.Username = newUsername;
         databaseLoaded.User.Passkeys = newPasskeys;
         databaseLoaded.User.LogoutTimeout = logoutTimeout;
         databaseLoaded.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
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

         // When
         databaseCreated.User.Username = newUsername;
         expectedLogs.Push($"User {oldUsername}'s username has been set to {newUsername}|True");
         databaseCreated.User.Passkeys = newPasskeys;
         expectedLogs.Push($"User {oldUsername}'s passkeys has been updated|True");
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         expectedLogs.Push($"User {oldUsername}'s logout timeout has been set to {logoutTimeout}|False");
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedLogs.Push($"User {oldUsername}'s cleaning clipboard timeout has been set to {cleaningClipboardTimeout}|False");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         databaseCreated.Save();
         expectedLogs.Push($"User {newUsername}'s database saved|False");
         databaseCreated.Close();
         expectedLogs.Push($"User {newUsername} logged out|False");
         expectedLogs.Push($"User {newUsername}'s database closed|False");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();

         // When
         IDatabase databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter, UnitTestsHelper.SerializationCenter, databaseFile, autoSaveFile, logFile, newUsername);
         expectedLogs.Push($"User {newUsername}'s database opened|False");
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedLogs.Push($"User {newUsername} logged in|False");

         // Then
         _ = databaseLoaded.User.Should().NotBeNull();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         _ = File.Exists(autoSaveFile).Should().BeFalse();

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

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

         // When
         databaseCreated.User.Username = newUsername;
         expectedLogs.Push($"User {oldUsername}'s username has been set to {newUsername}|True");
         databaseCreated.User.Passkeys = newPasskeys;
         expectedLogs.Push($"User {oldUsername}'s passkeys has been updated|True");
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         expectedLogs.Push($"User {oldUsername}'s logout timeout has been set to {logoutTimeout}|False");
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         expectedLogs.Push($"User {oldUsername}'s cleaning clipboard timeout has been set to {cleaningClipboardTimeout}|False");
         databaseCreated.Close();
         expectedLogs.Push($"User {oldUsername} logged out without saving|True");
         expectedLogs.Push($"User {oldUsername}'s database closed|False");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"User {oldUsername}'s database opened|False");
         expectedLogs.Push($"User {oldUsername} logged in|False");
         expectedLogs.Push($"User {oldUsername}'s autosave merged|True");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"User {newUsername} logged out|False");
         expectedLogs.Push($"User {newUsername}'s database closed|False");

         databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter, UnitTestsHelper.SerializationCenter, databaseFile, autoSaveFile, logFile, newUsername);
         expectedLogs.Push($"User {newUsername}'s database opened|False");
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }
         expectedLogs.Push($"User {newUsername} logged in|False");

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = databaseLoaded.User.Username.Should().Be(newUsername);
         _ = databaseLoaded.User.Passkeys.Should().BeEquivalentTo(newPasskeys);
         _ = databaseLoaded.User.LogoutTimeout.Should().Be(logoutTimeout);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
