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
      [Ignore]
      [TestMethod]
      public void Case00_GenerateNewDatabase()
      {
         UnitTestsHelper.ClearTestEnvironment("_");

         IDatabase database = UnitTestsHelper.CreateTestDatabase(["a", "b"], "_");
         IUser user = database.User;
         user.LogoutTimeout = 10;
         user.CleaningClipboardTimeout = 15;
         user.WarningsToNotify = (WarningType)0;
         string logFile = database.DatabaseFile.Replace(".pku", ".log");
         File.WriteAllText(logFile, string.Empty);

         for (int i = 0; i < 100; i++)
         {
            IService service = user.AddService($"Service{i} ({UnitTestsHelper.GetRandomString(min: 10, max: 15)})");
            service.Url = new Uri($"http://service{i}.xyz");
            int random = UnitTestsHelper.GetRandomInt(100) % 10;
            service.Notes = random == 0 ? $"Service{i} notes : \n{UnitTestsHelper.GetRandomString(min: 10, max: 150)}" : "";

            int accountNumber = UnitTestsHelper.GetRandomInt(min: 1, max: 5);

            for (int j = 0; j < accountNumber; j++)
            {
               random = UnitTestsHelper.GetRandomInt(10) + 1;

               IAccount account;
               string password = UnitTestsHelper.GetRandomString(min: 20, max: 25);
               switch (random % 4)
               {
                  case 1:
                     account = service.AddAccount(label: $"Account{j}",
                        identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => $"👤{x}@test.te"));
                     break;
                  case 2:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => $"👤{x}@test.te"),
                        password: password);
                     break;
                  case 3:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => $"👤{x}@test.te"));
                     break;
                  default:
                     account = service.AddAccount(label: $"Account{j}",
                        identifiers: UnitTestsHelper.GetRandomStringArray(random / 2).Select(x => $"👤{x}@test.te"),
                        password: password);
                     break;
               }

               random = UnitTestsHelper.GetRandomInt(100);
               account.Notes = random % 10 == 0 ? $"Service{i}'s Account{j} notes : \n{UnitTestsHelper.GetRandomString(min: 10, max: 150)}" : "";
               account.PasswordUpdateReminderDelay = random < 10 ? random : 0;
               account.Options = (!string.IsNullOrEmpty(account.Password) && random % 2 == 0) ? AccountOption.WarnIfPasswordLeaked : AccountOption.None;
               File.AppendAllText(logFile, "#");
            }
            File.AppendAllText(logFile, "\n");
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
         Stack<string> expectedActivities = new();

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         expectedActivities.Push($"Information : {databaseCreated.User}'s database created");

         // Then
         _ = databaseCreated.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseCreated.DatabaseFile).Should().BeTrue();

         _ = databaseCreated.User.Should().NotBeNull();
         _ = databaseCreated.User.Username.Should().Be(username);

         _ = databaseCreated.User.LogoutTimeout.Should().Be(0);
         _ = databaseCreated.User.CleaningClipboardTimeout.Should().Be(0);

         // When
         databaseCreated.Close();
         expectedActivities.Push($"Information : User {username} logged out");
         expectedActivities.Push($"Information : User {username}'s database closed");

         // Then
         _ = databaseCreated.User.Should().BeNull();
         _ = File.Exists(databaseFile).Should().BeTrue();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedActivities.Push($"Information : {databaseLoaded.User}'s database opened");
         expectedActivities.Push($"Information : {databaseLoaded.User} logged in");

         // Then
         _ = databaseLoaded.Should().NotBeNull();
         _ = databaseLoaded.DatabaseFile.Should().Be(databaseFile);
         _ = File.Exists(databaseLoaded.DatabaseFile).Should().BeTrue();

         _ = databaseLoaded.User.Should().NotBeNull();
         _ = databaseLoaded.User.Username.Should().Be(username);

         _ = databaseLoaded.User.LogoutTimeout.Should().Be(0);
         _ = databaseLoaded.User.CleaningClipboardTimeout.Should().Be(0);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);

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
         Stack<string> expectedActivities = new();
         Stack<string> expectedLogWarnings = new();

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(wrongPasskeys, out _);
         expectedActivities.Push($"Information : User {username}'s database opened");
         for (int i = wrongKeyIndex; i < wrongPasskeys.Length; i++)
         {
            expectedActivities.Push($"Warning : User {username} login failed at level {wrongKeyIndex + 1}");
            expectedLogWarnings.Push($"Warning : User {username} login failed at level {wrongKeyIndex + 1}");
         }

         // Then
         _ = databaseLoaded.User.Should().BeNull();

         // When
         databaseLoaded.Close();
         expectedActivities.Push($"Information : User {username}'s database closed");
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedActivities.Push($"Information : User {username}'s database opened");
         expectedActivities.Push($"Information : User {username} logged in");

         // Then
         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedActivities = new();
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
         _ = database.Activities.FirstOrDefault(x => x.Message == $"User {username}'s login session timeout reached" && x.NeedsReview).Should().NotBeNull();

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A database created and closed normally opens without any tampering warning,
       * Then stripping the activity-log signature is detected on the next login.
      */
      public void Case06_ActivityLogTamperingIsDetected()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string tamperMessage = $"User {username}'s activity log integrity check failed";

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When (untampered)
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (no tampering detected)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage).Should().BeFalse();

         // When (tampered: the sealed signature is stripped from the log)
         databaseLoaded.Close();
         UnitTestsHelper.TamperActivityLogSignature(databaseFile);
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (tampering detected and flagged for review)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage && x.NeedsReview).Should().BeTrue();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Truncating (rolling back) the sealed portion of the activity log is
       * detected on the next login: the stored list becomes shorter than the
       * count it claims to have sealed.
      */
      public void Case07_ActivityLogTruncationIsDetected()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string tamperMessage = $"User {username}'s activity log integrity check failed";

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When (untampered)
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (no tampering detected)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage).Should().BeFalse();

         // When (tampered: one sealed entry is removed from the log)
         databaseLoaded.Close();
         UnitTestsHelper.TamperActivityLogTruncate(databaseFile);
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (tampering detected and flagged for review)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage && x.NeedsReview).Should().BeTrue();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Substituting the log's public key is detected on the next login: the
       * private key anchoring verification (held in the tamper-proof database)
       * no longer matches the public key stored in the log.
      */
      public void Case08_ActivityLogKeySubstitutionIsDetected()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string tamperMessage = $"User {username}'s activity log integrity check failed";

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When (untampered)
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (no tampering detected)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage).Should().BeFalse();

         // When (tampered: the log's public key is swapped for an attacker's)
         databaseLoaded.Close();
         UnitTestsHelper.TamperActivityLogPublicKey(databaseFile);
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (tampering detected and flagged for review)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage && x.NeedsReview).Should().BeTrue();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Reordering the sealed entries is detected on the next login: the
       * signature no longer matches the canonical content it was computed over,
       * even though nothing was added or removed.
      */
      public void Case09_ActivityLogReorderingIsDetected()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string tamperMessage = $"User {username}'s activity log integrity check failed";

         UnitTestsHelper.ClearTestEnvironment();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         // When (untampered)
         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (no tampering detected)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage).Should().BeFalse();

         // When (tampered: two sealed entries are swapped)
         databaseLoaded.Close();
         UnitTestsHelper.TamperActivityLogReorder(databaseFile);
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);

         // Then (tampering detected and flagged for review)
         _ = databaseLoaded.Activities.Any(x => x.Message == tamperMessage && x.NeedsReview).Should().BeTrue();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * The asynchronous entry points drive the very same pipeline as their
       * synchronous twins: CreateAsync then SaveAsync persist an update, and
       * OpenAsync followed by one LoginAsync per passkey reads it back.
      */
      public async Task Case10_AsynchronousEntryPointsRoundTrip()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();

         UnitTestsHelper.ClearTestEnvironment();

         // When
         IDatabase databaseCreated = await Database.CreateAsync(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            UnitTestsHelper.ClipboardManager,
            databaseFile,
            username,
            passkeys);

         databaseCreated.User.WarningsToNotify = (WarningType)0;
         databaseCreated.User.NumberOfOldPasswordToKeep = 7;

         await databaseCreated.SaveAsync();
         databaseCreated.Close();

         IDatabase databaseLoaded = await Database.OpenAsync(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            UnitTestsHelper.ClipboardManager,
            databaseFile,
            username);

         IUser? user = null;

         foreach (string passkey in passkeys)
         {
            user = await databaseLoaded.LoginAsync(passkey);
         }

         // Then
         _ = user.Should().NotBeNull();
         _ = user.Username.Should().Be(username);
         _ = user.NumberOfOldPasswordToKeep.Should().Be(7);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Only the last LoginAsync call, once every passkey has been provided in
       * order, returns the user: the progressive stack behaves exactly as the
       * synchronous Login does.
      */
      public async Task Case11_AsynchronousLoginIsProgressive()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(3);
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();

         UnitTestsHelper.ClearTestEnvironment();

         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         databaseCreated.Close();

         IDatabase databaseLoaded = await Database.OpenAsync(UnitTestsHelper.CryptographicCenter,
            UnitTestsHelper.SerializationCenter,
            UnitTestsHelper.PasswordFactory,
            UnitTestsHelper.ClipboardManager,
            databaseFile,
            username);

         // When / Then
         for (int i = 0; i < passkeys.Length - 1; i++)
         {
            _ = (await databaseLoaded.LoginAsync(passkeys[i])).Should().BeNull();
         }

         _ = (await databaseLoaded.LoginAsync(passkeys[^1])).Should().NotBeNull();

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
