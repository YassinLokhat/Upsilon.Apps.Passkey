using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class ServiceUnitTests
   {
      [TestMethod]
      /*
       * User.AddService Adds the new service,
       * Then updating the service,
       * Then Database.Save will save the update in the database file and delete the autosave file,
       * Then GetUser loads correctly the updated database file.
      */
      public void Case01_ServiceAdd()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();
         string oldDatabaseContent = File.ReadAllText(UnitTestsHelper.ComputeDatabaseFilePath());
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);

         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomPasskeys();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseLoaded.User));

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
       * Then GetUser loads correctly the updated database file.
      */
      public void Case02_UserUpdateThenSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase();
         string newUsername = UnitTestsHelper.GetRandomString();
         string[] newPasskeys = UnitTestsHelper.GetRandomPasskeys();
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         databaseCreated.Save();
         databaseCreated.Close();

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();

         // When
         IDatabase databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter, UnitTestsHelper.SerializationCenter, databaseFile, autoSaveFile, logFile, newUsername);
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }

         // Then
         _ = databaseLoaded.User.Should().NotBeNull();
         _ = (databaseLoaded.User?.Username.Should().Be(newUsername));
         _ = (databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout));
         _ = (databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout));

         _ = File.Exists(autoSaveFile).Should().BeFalse();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Updating User creates an autosave file,
       * Then GetUser loads the database file without the updated data,
       * Then HandleAutoSave updates the database object and the database file,
       * Then GetUser loads correctly the updated database file.
      */
      public void Case03_UserUpdateButNotSaved()
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
         int logoutTimeout = UnitTestsHelper.GetRandomInt(1, 60);
         int cleaningClipboardTimeout = UnitTestsHelper.GetRandomInt(1, 60);

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         databaseCreated.User.Username = newUsername;
         databaseCreated.User.Passkeys = newPasskeys;
         databaseCreated.User.LogoutTimeout = logoutTimeout;
         databaseCreated.User.CleaningClipboardTimeout = cleaningClipboardTimeout;
         databaseCreated.Close();

         // Then
         _ = File.Exists(autoSaveFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(oldPasskeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = (databaseLoaded.User?.Username.Should().Be(newUsername));
         _ = (databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(newPasskeys));
         _ = (databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout));
         _ = (databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout));

         // When
         databaseLoaded.Close();
         databaseLoaded = IDatabase.Open(UnitTestsHelper.CryptographicCenter, UnitTestsHelper.SerializationCenter, databaseFile, autoSaveFile, logFile, newUsername);
         foreach (string passkey in newPasskeys)
         {
            _ = databaseLoaded.Login(passkey);
         }

         // Then
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = (databaseLoaded.User?.Username.Should().Be(newUsername));
         _ = (databaseLoaded.User?.Passkeys.Should().BeEquivalentTo(newPasskeys));
         _ = (databaseLoaded.User?.LogoutTimeout.Should().Be(logoutTimeout));
         _ = (databaseLoaded.User?.CleaningClipboardTimeout.Should().Be(cleaningClipboardTimeout));

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
