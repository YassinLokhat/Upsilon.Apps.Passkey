using FluentAssertions;
using Upsilon.Apps.PassKey.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Upsilon.Apps.PassKey.Core.Models;

namespace Upsilon.Apps.PassKey.UnitTests.Models
{
   [TestClass]
   public sealed class DatabaseUnitTests
   {
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
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         Stack<string> expectedLogs = new();

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         expectedLogs.Push($"Information : User {username}'s database created");
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         // Then
         _ = databaseCreated.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseCreated.DatabaseFile).Should().BeTrue();

         _ = databaseCreated.AutoSaveFile.Should().Be(autoSaveFile);
         _ = File.Exists(databaseCreated.AutoSaveFile).Should().BeFalse();

         _ = databaseCreated.LogFile.Should().Be(logFile);
         _ = File.Exists(databaseCreated.LogFile).Should().BeTrue();

         _ = databaseCreated.User.Should().NotBeNull();
         _ = databaseCreated.User.Username.Should().Be(username);

         _ = databaseCreated.User.LogoutTimeout.Should().Be(0);
         _ = databaseCreated.User.CleaningClipboardTimeout.Should().Be(0);

         // When
         databaseCreated.Close();
         expectedLogs.Push($"Information : User {username} logged out");
         expectedLogs.Push($"Information : User {username}'s database closed");

         // Then
         _ = databaseCreated.User.Should().BeNull();
         _ = File.Exists(databaseFile).Should().BeTrue();
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = File.Exists(logFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         // Then
         _ = databaseLoaded.Should().NotBeNull();
         _ = databaseLoaded.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseLoaded.DatabaseFile).Should().BeTrue();

         _ = databaseLoaded.AutoSaveFile.Should().Be(autoSaveFile);
         _ = File.Exists(databaseLoaded.AutoSaveFile).Should().BeFalse();

         _ = databaseLoaded.LogFile.Should().Be(logFile);
         _ = File.Exists(databaseLoaded.LogFile).Should().BeTrue();

         _ = databaseLoaded.User.Should().NotBeNull();
         _ = databaseLoaded.User.Username.Should().Be(username);

         _ = databaseLoaded.User.LogoutTimeout.Should().Be(0);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // When
         databaseLoaded.Delete();

         // Then
         _ = databaseCreated.User.Should().BeNull();
         _ = File.Exists(databaseFile).Should().BeFalse();
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = File.Exists(logFile).Should().BeFalse();

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
         databaseCreated.Close();
         IOException exception = null;
         IDatabase newDatabase = null;

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
         _ = act.Should().Throw<IOException>();
         _ = newDatabase.Should().BeNull();
         _ = exception.Should().NotBeNull();
         _ = exception.Message.Should().Be($"'{UnitTestsHelper.ComputeDatabaseFilePath()}' database file already exists");

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
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IOException exception = null;
         IDatabase databaseLoaded = null;

         // When
         Action act = new(() =>
         {
            try
            {
               databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
            }
            catch (IOException ex)
            {
               exception = ex;
               throw;
            }
         });

         // Then
         _ = act.Should().Throw<IOException>();
         _ = databaseLoaded.Should().BeNull();
         _ = exception.Should().NotBeNull();

         // When
         databaseCreated.Close();
         exception = null;
         act = new(() =>
         {
            try
            {
               databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
            }
            catch (IOException ex)
            {
               exception = ex;
               throw;
            }
         });

         // Then
         _ = act.Should().NotThrow<IOException>();
         _ = exception.Should().BeNull();
         _ = databaseLoaded.Should().NotBeNull();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Database.Login don't return any User if wrong passkeys is provided.
      */
      public void Case04_DatabaseOpenButWrongPasskeysProvided()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string[] wrongPasskeys = [.. passkeys];
         int wrongKeyIndex = UnitTestsHelper.GetRandomInt(passkeys.Length);
         wrongPasskeys[wrongKeyIndex] = UnitTestsHelper.GetRandomString();
         Stack<string> expectedLogs = new();
         Stack<string> expectedLogWarnings = new();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(wrongPasskeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         for (int i = wrongKeyIndex; i < wrongPasskeys.Length; i++)
         {
            expectedLogs.Push($"Warning : User {username} login failed at level {wrongKeyIndex + 1}");
            expectedLogWarnings.Push($"Warning : User {username} login failed at level {wrongKeyIndex + 1}");
         }

         // Then
         _ = databaseLoaded.User.Should().BeNull();

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"Information : User {username}'s database closed");
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         // Then
         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Database autmatically closes when timeout reached and Database.DatabaseClosed event rized with the correct eventarg.
      */
      public void Case05_DatabaseAutoLogout()
      {
         // Given
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         bool closedDueToTimeout = false;
         Stack<string> expectedLogs = new();
         Stack<string> expectedLogWarnings = new();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase database = Database.Create(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            databaseFile,
            autoSaveFile,
            logFile,
            username,
            passkeys);

         database.DatabaseClosed += (s, e) => { closedDueToTimeout = e.LoginTimeoutReached; };

         foreach (string passkey in passkeys)
         {
            _ = database.Login(passkey);
         }

         database.User.LogoutTimeout = 1;
         database.Save();
         DateTime start = DateTime.Now;

         // When
         for (int i = 0; !closedDueToTimeout && i < 300; i++)
         {
            Thread.Sleep(500);
         }

         int elapsedTime = (int)(DateTime.Now - start).TotalMinutes;

         // Then
         _ = closedDueToTimeout.Should().BeTrue();

         // When
         database = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then
         elapsedTime.Should().Be(database.User.LogoutTimeout);
         database.Logs.FirstOrDefault(x => x.Message == $"User {username}'s login session timeout reached" && x.NeedsReview).Should().NotBeNull();

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
