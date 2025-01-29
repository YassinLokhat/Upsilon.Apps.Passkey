using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.UnitTests.Models
{
   [TestClass]
   public sealed class ServiceUnitTests
   {
      [TestMethod]
      /*
       * User.AddService adds the new service,
       * Then updating the service and saving will save the update in the database file and delete the autosave file,
       * Then Database.Open loads correctly the updated database file with the updated service.
      */
      public void Case01_AddServiceUpdateSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string oldServiceName = "Service_" + UnitTestsHelper.GetUsername();
         string newServiceName = "new_" + oldServiceName;
         string url = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         Stack<string> expectedLogs = new();

         // When
         IService service = databaseCreated.User.AddService(oldServiceName);
         expectedLogs.Push($"Service {oldServiceName} has been added to User {username}|False");

         // Then
         _ = databaseCreated.User.Services.Length.Should().Be(1);

         // When
         service.ServiceName = newServiceName;
         expectedLogs.Push($"Service {oldServiceName}'s service name has been set to {newServiceName}|True");
         service.Url = url;
         expectedLogs.Push($"Service {newServiceName}'s url has been set to {url}|False");
         service.Notes = notes;
         expectedLogs.Push($"Service {newServiceName}'s notes has been set to {notes}|False");

         databaseCreated.Save();
         expectedLogs.Push($"User {username}'s database saved|False");
         databaseCreated.Close();
         expectedLogs.Push($"User {username} logged out|False");
         expectedLogs.Push($"User {username}'s database closed|False");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.ServiceName.Should().Be(newServiceName);
         _ = serviceLoaded.Url.Should().Be(url);
         _ = serviceLoaded.Notes.Should().Be(notes);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * User.AddService adds the new service,
       * Then updating the service without saving will create the autosave file,
       * Then Database.Open with AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile loads correctly the updated database file with the updated service.
      */
      public void Case02_AddServiceUpdateAutoSave()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string oldServiceName = "Service_" + UnitTestsHelper.GetUsername();
         string newServiceName = "new_" + oldServiceName;
         string url = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         Stack<string> expectedLogs = new();

         // When
         IService service = databaseCreated.User.AddService(oldServiceName);
         expectedLogs.Push($"Service {oldServiceName} has been added to User {username}|False");

         // Then
         _ = databaseCreated.User.Services.Length.Should().Be(1);

         // When
         service.ServiceName = newServiceName;
         expectedLogs.Push($"Service {oldServiceName}'s service name has been set to {newServiceName}|True");
         service.Url = url;
         expectedLogs.Push($"Service {newServiceName}'s url has been set to {url}|False");
         service.Notes = notes;
         expectedLogs.Push($"Service {newServiceName}'s notes has been set to {notes}|False");

         databaseCreated.Close();
         expectedLogs.Push($"User {username} logged out without saving|True");
         expectedLogs.Push($"User {username}'s database closed|False");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");
         expectedLogs.Push($"User {username}'s autosave merged|True");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.ServiceName.Should().Be(newServiceName);
         _ = serviceLoaded.Url.Should().Be(url);
         _ = serviceLoaded.Notes.Should().Be(notes);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * User.DeleteService deletes the service,
       * Then Database.Open loads correctly the updated database file with the updated service.
      */
      public void Case03_DeleteServiceUpdateSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         _ = databaseCreated.User.AddService(serviceName);
         databaseCreated.Save();
         databaseCreated.Close();
         Stack<string> expectedLogs = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);
         expectedLogs.Push($"Service {serviceName} has been removed from User {username}|True");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         // When
         databaseLoaded.Save();
         expectedLogs.Push($"User {username}'s database saved|False");
         databaseLoaded.Close();
         expectedLogs.Push($"User {username} logged out|False");
         expectedLogs.Push($"User {username}'s database closed|False");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * User.DeleteService adeletes the service,
       * Then Database.Open with AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile loads correctly the updated database file with the updated service.
      */
      public void Case04_DeleteServiceUpdateAutoSave()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         _ = databaseCreated.User.AddService(serviceName);
         databaseCreated.Close();
         Stack<string> expectedLogs = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);
         expectedLogs.Push($"Service {serviceName} has been removed from User {username}|True");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"User {username} logged out without saving|True");
         expectedLogs.Push($"User {username}'s database closed|False");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");
         expectedLogs.Push($"User {username}'s autosave merged|True");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
