﻿using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Interfaces;

namespace Upsilon.Apps.Passkey.UnitTests.Models
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

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);

         // Then
         _ = databaseCreated.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseCreated.DatabaseFile).Should().BeTrue();

         _ = databaseCreated.AutoSaveFile.Should().Be(autoSaveFile);
         _ = File.Exists(databaseCreated.AutoSaveFile).Should().BeFalse();

         _ = databaseCreated.LogFile.Should().Be(logFile);
         _ = File.Exists(databaseCreated.LogFile).Should().BeFalse();

         _ = databaseCreated.User.Should().NotBeNull();
         _ = (databaseCreated.User?.Username.Should().Be(username));

         _ = (databaseCreated.User?.LogoutTimeout.Should().Be(0));
         _ = (databaseCreated.User?.CleaningClipboardTimeout.Should().Be(0));

         // When
         databaseCreated.Close();

         // Then
         _ = databaseCreated.User.Should().BeNull();
         _ = File.Exists(databaseFile).Should().BeTrue();
         _ = File.Exists(autoSaveFile).Should().BeFalse();
         _ = File.Exists(logFile).Should().BeFalse();

         // When
         IDatabase? databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);

         // Then
         _ = databaseLoaded.Should().NotBeNull();
         _ = (databaseLoaded?.DatabaseFile.Should().Be(databaseFile));
         _ = File.Exists(databaseLoaded?.DatabaseFile).Should().BeTrue();

         _ = (databaseLoaded?.AutoSaveFile.Should().Be(autoSaveFile));
         _ = File.Exists(databaseLoaded?.AutoSaveFile).Should().BeFalse();

         _ = (databaseLoaded?.LogFile.Should().Be(logFile));
         _ = File.Exists(databaseLoaded?.LogFile).Should().BeFalse();

         _ = (databaseLoaded?.User.Should().NotBeNull());
         _ = (databaseLoaded?.User?.Username.Should().Be(username));

         _ = (databaseLoaded?.User?.LogoutTimeout.Should().Be(0));
         _ = (databaseLoaded?.User?.CleaningClipboardTimeout.Should().Be(0));

         // When
         databaseLoaded?.Delete();

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
         _ = act.Should().Throw<IOException>();
         _ = newDatabase.Should().BeNull();
         _ = exception.Should().NotBeNull();
         _ = (exception?.Message.Should().Be($"'{UnitTestsHelper.ComputeDatabaseFilePath()}' database file already exists"));

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
         IOException? exception = null;
         IDatabase? databaseLoaded = null;

         // When
         Action act = new(() =>
         {
            try
            {
               databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
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
               databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
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
         databaseLoaded?.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Database.Login don't return any User if wrong passkeys is provided.
      */
      public void Case04_DatabaseOpenButWrongPasskeysProvided()
      {
         // Given
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string[] wrongPasskeys = [.. passkeys];
         int wrongKeyIndex = UnitTestsHelper.GetRandomInt(passkeys.Length);
         wrongPasskeys[wrongKeyIndex] = UnitTestsHelper.GetRandomString();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(wrongPasskeys);

         // Then
         _ = databaseLoaded.User.Should().BeNull();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
