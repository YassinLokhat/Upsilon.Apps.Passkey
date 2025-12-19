using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class DatabaseUnitTests
   {
      [TestMethod, Ignore]
      public void Case00_GenerateNewDatabase()
      {
         IDatabase database = UnitTestsHelper.CreateTestDatabase(["a", "b"], "_");
         IUser user = database.User;
         user.LogoutTimeout = 10;
         user.CleaningClipboardTimeout = 15;
         user.WarningsToNotify = (WarningType)0;

         for (int i = 0; i < 50; i++)
         {
            IService service = user.AddService($"Service{i} ({UnitTestsHelper.GetRandomString(min: 10, max: 15)})");
            service.Url = $"www.service{i}.xyz";
            int random = UnitTestsHelper.GetRandomInt(100) % 10;
            service.Notes = random == 0 ? $"Service{i} notes : \n{UnitTestsHelper.GetRandomString(min: 10, max: 150)}" : "";

            int accountNumber = UnitTestsHelper.GetRandomInt(min: 1, max: 5);

            for (int j = 0; j < accountNumber; j++)
            {
               random = UnitTestsHelper.GetRandomInt(10) + 1;

               IAccount account;
               switch (random % 4)
               {
                  case 1:
                     account = service.AddAccount(label: $"Account{j}",
                        identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => x + "@test.te"));
                     break;
                  case 2:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => x + "@test.te"),
                        password: UnitTestsHelper.GetRandomString(min: 20, max: 25));
                     break;
                  case 3:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => x + "@test.te"));
                     break;
                  default:
                     account = service.AddAccount(label: $"Account{j}",
                        identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => x + "@test.te"),
                        password: UnitTestsHelper.GetRandomString(min: 20, max: 25));
                     break;
               }

               random = UnitTestsHelper.GetRandomInt(100);
               account.Notes = random % 10 == 0 ? $"Service{i}'s Account{j} notes : \n{UnitTestsHelper.GetRandomString(min: 10, max: 150)}" : "";
               account.PasswordUpdateReminderDelay = random < 10 ? random : 0;
               account.Options = random % 2 == 0 ? AccountOption.WarnIfPasswordLeaked : AccountOption.None;
            }
         }

         database.Save();
         database.Close();
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
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         Stack<string> expectedLogs = new();

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         expectedLogs.Push($"Information : {databaseCreated.User}'s database created");

         // Then
         _ = databaseCreated.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseCreated.DatabaseFile).Should().BeTrue();

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

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : {databaseLoaded.User}'s database opened");
         expectedLogs.Push($"Information : {databaseLoaded.User} logged in");

         // Then
         _ = databaseLoaded.Should().NotBeNull();
         _ = databaseLoaded.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseLoaded.DatabaseFile).Should().BeTrue();

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
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         bool closedDueToTimeout = false;
         Stack<string> expectedLogs = new();
         Stack<string> expectedLogWarnings = new();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase database = Database.Create(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            UnitTestsHelper.ClipboardManager,
            databaseFile,
            username,
            passkeys);

         database.DatabaseClosed += (s, e) => { closedDueToTimeout = e.LoginTimeoutReached; };

         database.User.LogoutTimeout = 1;
         database.Save();
         DateTime start = DateTime.Now;

         // When
         for (int i = 0; !closedDueToTimeout && i < 300; i++)
         {
            Thread.Sleep(500);
         }

         // Then
         _ = closedDueToTimeout.Should().BeTrue();

         // When
         database = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then
         _ = database.Logs.FirstOrDefault(x => x.Message == $"User {username}'s login session timeout reached" && x.NeedsReview).Should().NotBeNull();

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
