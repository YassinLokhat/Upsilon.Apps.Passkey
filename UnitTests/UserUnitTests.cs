using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.Passkey.Core.Models;

namespace Upsilon.Apps.Passkey.UnitTests
{
   [TestClass]
   public sealed class UserUnitTests
   {
      [TestMethod]
      /*
       * 
      */
      public void Case0()
      {

      }

      [TestMethod]
      /*
       * Database.Create creates an empty database file,
       * Then Database.Dispose releases correctly the database file,
       * Then Database.Open loads correctly the database file,
       * Then Database.Delete deletes correctly the database file.
      */
      public void Case01_DatabaseCreationOpenDelete()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);

         // Then
         databaseCreated.DatabaseFile.Should().Be(databaseFile);
         File.Exists(databaseCreated.DatabaseFile).Should().BeTrue();

         databaseCreated.AutoSaveFile.Should().Be(autoSaveFile);
         File.Exists(databaseCreated.AutoSaveFile).Should().BeFalse();

         databaseCreated.LogFile.Should().Be(logFile);
         File.Exists(databaseCreated.LogFile).Should().BeFalse();

         databaseCreated.User.Should().NotBeNull();
         databaseCreated.User?.Username.Should().Be(username);
         databaseCreated.User?.Passkeys.Should().BeEquivalentTo(passkeys);

         databaseCreated.User?.PasswordTimeout.Should().Be(0);
         databaseCreated.User?.LogoutTimeout.Should().Be(0);
         databaseCreated.User?.CleaningClipboardTimeout.Should().Be(0);

         // When
         databaseCreated.Dispose();

         // Then
         databaseCreated.User.Should().BeNull();
         File.Exists(databaseFile).Should().BeTrue();
         File.Exists(autoSaveFile).Should().BeFalse();
         File.Exists(logFile).Should().BeFalse();

         // When
         IDatabase? databaseLoaded = Database.Open(databaseFile, autoSaveFile, logFile, username, passkeys);

         // Then
         databaseLoaded.Should().NotBeNull();
         databaseLoaded?.DatabaseFile.Should().Be(databaseFile);
         File.Exists(databaseLoaded?.DatabaseFile).Should().BeTrue();

         databaseLoaded?.AutoSaveFile.Should().Be(autoSaveFile);
         File.Exists(databaseLoaded?.AutoSaveFile).Should().BeFalse();

         databaseLoaded?.LogFile.Should().Be(logFile);
         File.Exists(databaseLoaded?.LogFile).Should().BeFalse();

         databaseLoaded?.User.Should().NotBeNull();
         databaseLoaded?.User?.Username.Should().Be(username);
         databaseLoaded?.User?.Passkeys.Should().BeEquivalentTo(passkeys);

         databaseLoaded?.User?.PasswordTimeout.Should().Be(0);
         databaseLoaded?.User?.LogoutTimeout.Should().Be(0);
         databaseLoaded?.User?.CleaningClipboardTimeout.Should().Be(0);

         // When
         databaseLoaded?.Delete();

         // Then
         databaseCreated.User.Should().BeNull();
         File.Exists(databaseFile).Should().BeFalse();
         File.Exists(autoSaveFile).Should().BeFalse();
         File.Exists(logFile).Should().BeFalse();

         // Finaly
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Database.Create throws an error if the database file already exists.
      */
      public void Case02_DatabaseCreationButAlreadyExists()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase();
         databaseCreated.Dispose();
         IOException? exception = null;
         IDatabase? newDatabase = null;

         // When
         Action act = new(() =>
         {
            try
            {
               newDatabase = UnitTestsHelper.CreateTestDatabase();
            }
            catch (IOException ex)
            {
               exception = ex;
               throw;
            }
         });

         // Then
         act.Should().Throw<IOException>();
         newDatabase.Should().BeNull();
         exception.Should().NotBeNull();
         exception?.Message.Should().Be($"'{UnitTestsHelper.ComputeDatabaseFilePath()}' database file already exists");

         // Finaly
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Database.Open throws an error if the database file is already opened,
       * Then Database.Open works again after Database.Dispose released the database file.
      */
      public void Case03_DatabaseOpenButAlreadyOpened()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IOException? exception = null;
         IDatabase? databaseLoaded = null;

         // When
         Action act = new(() =>
         {
            try
            {
               databaseLoaded = Database.Open(databaseFile,
                  autoSaveFile,
                  logFile,
                  username,
                  passkeys);
            }
            catch (IOException ex)
            {
               exception = ex;
               throw;
            }
         });

         // Then
         act.Should().Throw<IOException>();
         databaseLoaded.Should().BeNull();
         exception.Should().NotBeNull();

         // When
         databaseCreated.Dispose();
         exception = null;
         act = new(() =>
         {
            try
            {
               databaseLoaded = Database.Open(databaseFile,
                  autoSaveFile,
                  logFile,
                  username,
                  passkeys);
            }
            catch (IOException ex)
            {
               exception = ex;
               throw;
            }
         });

         // Then
         act.Should().NotThrow<IOException>();
         exception.Should().BeNull();
         databaseLoaded.Should().NotBeNull();

         // Finaly
         databaseLoaded?.Dispose();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file and don't update the save file.
      */
      public void Case04_UserUpdateWithoutSaving()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Dispose();
         string oldDatabaseContent = File.ReadAllText(UnitTestsHelper.ComputeDatabaseFilePath());
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);

         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomPasskeys();
         int passwordTimer = UnitTestsHelper.GetRandomInt(1, 60);
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseLoaded.User));

         databaseLoaded.User.Username = newUsername;
         databaseLoaded.User.Passkeys = newPasskeys;
         databaseLoaded.User.PasswordTimeout = passwordTimer;
         databaseLoaded.User.LogoutTimeout = logoutTimeout;
         databaseLoaded.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseLoaded.Dispose();

         // Then
         File.Exists(UnitTestsHelper.ComputeAutoSaveFilePath()).Should().BeTrue();
         File.ReadAllText(UnitTestsHelper.ComputeDatabaseFilePath()).Should().Be(oldDatabaseContent);

         // Finaly
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then Database.Save will save the update in the database file and delete the autosave file,
       * Then GetUser loads correctly the updated database file.
      */
      public void Case05_UserUpdateThenSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase();
         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomPasskeys();
         int passwordTimer = UnitTestsHelper.GetRandomInt(1, 60);
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.PasswordTimeout = passwordTimer;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;

         // Then
         File.Exists(autoSaveFile).Should().BeTrue();

         // When
         databaseCreated.Save();
         databaseCreated.Dispose();

         // Then
         File.Exists(autoSaveFile).Should().BeFalse();

         // When
         IDatabase databaseLoaded = Database.Open(databaseFile, autoSaveFile, logFile, newUsername, newPasskeys);

         // Then
         databaseLoaded.User.Should().NotBeNull();
         databaseLoaded.User?.Username.Should().Be(newUsername);
         databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(newPasskeys);
         databaseLoaded.User?.PasswordTimeout.Should().Be(passwordTimer);
         databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout);
         databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         File.Exists(autoSaveFile).Should().BeFalse();

         // Finaly
         databaseLoaded.Dispose();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then GetUser loads the database file without the updated data,
       * Then HandleAutoSave updates the database object and the database file,
       * Then GetUser loads correctly the updated database file.
      */
      public void Case06_UserUpdateButNotSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string oldUsername = UnitTestsHelper.GetUsername();
         string[] oldPasskeys = UnitTestsHelper.GetRandomPasskeys();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(oldPasskeys);
         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomPasskeys();
         int passwordTimer = UnitTestsHelper.GetRandomInt(1, 60);
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.PasswordTimeout = passwordTimer;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.Dispose();

         // Then
         File.Exists(autoSaveFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = Database.Open(databaseFile, autoSaveFile, logFile, oldUsername, oldPasskeys);

         // Then
         databaseLoaded.User.Should().NotBeNull();
         databaseLoaded.User?.Username.Should().Be(oldUsername);
         databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(oldPasskeys);
         databaseLoaded.User?.PasswordTimeout.Should().Be(0);
         databaseLoaded.User?.LogoutTimeout.Should().Be(0);
         databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(0);

         // When
         databaseLoaded.HandleAutoSave(mergeAutoSave: true);

         // Then
         File.Exists(autoSaveFile).Should().BeFalse();
         databaseLoaded.User?.Username.Should().Be(newUsername);
         databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(newPasskeys);
         databaseLoaded.User?.PasswordTimeout.Should().Be(passwordTimer);
         databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout);
         databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         // When
         databaseLoaded.Dispose();
         databaseLoaded = Database.Open(databaseFile, autoSaveFile, logFile, newUsername, newPasskeys);

         // // Then
         File.Exists(autoSaveFile).Should().BeFalse();
         databaseLoaded.User?.Username.Should().Be(newUsername);
         databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(newPasskeys);
         databaseLoaded.User?.PasswordTimeout.Should().Be(passwordTimer);
         databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout);
         databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout);

         // Finaly
         databaseLoaded.Dispose();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
