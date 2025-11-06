using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Public.Utils;
using Upsilon.Apps.PassKey.Core.Public.Enums;
using Upsilon.Apps.PassKey.Core.Public.Interfaces;

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
         Stack<string> expectedLogWarnings = new();

         // When
         IService service = databaseCreated.User.AddService(oldServiceName);
         expectedLogs.Push($"Information : Service {oldServiceName} has been added to User {username}");

         // Then
         databaseCreated.User.HasChanged().Should().BeTrue();
         service.HasChanged().Should().BeFalse();
         _ = databaseCreated.User.Services.Length.Should().Be(1);

         // When
         service.ServiceName = newServiceName;
         service.ServiceName = newServiceName;
         expectedLogs.Push($"Warning : Service {oldServiceName}'s service name has been set to {newServiceName}");
         expectedLogWarnings.Push($"Warning : Service {oldServiceName}'s service name has been set to {newServiceName}");
         service.Url = url;
         service.Url = url;
         expectedLogs.Push($"Information : Service {newServiceName}'s url has been set to {url}");
         service.Notes = notes;
         service.Notes = notes;
         expectedLogs.Push($"Information : Service {newServiceName}'s notes has been set to {notes}");

         // Then
         databaseCreated.User.HasChanged().Should().BeTrue();
         service.HasChanged().Should().BeTrue();
         service.HasChanged(nameof(service.ServiceName)).Should().BeTrue();
         service.HasChanged(nameof(service.Url)).Should().BeTrue();
         service.HasChanged(nameof(service.Notes)).Should().BeTrue();

         // When
         databaseCreated.Save();
         expectedLogs.Push($"Information : User {username}'s database saved");
         databaseCreated.Close();
         expectedLogs.Push($"Information : User {username} logged out");
         expectedLogs.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.ServiceName.Should().Be(newServiceName);
         _ = serviceLoaded.Url.Should().Be(url);
         _ = serviceLoaded.Notes.Should().Be(notes);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         // When
         IService service = databaseCreated.User.AddService(oldServiceName);
         expectedLogs.Push($"Information : Service {oldServiceName} has been added to User {username}");

         // Then
         _ = databaseCreated.User.Services.Length.Should().Be(1);

         // When
         service.ServiceName = newServiceName;
         service.ServiceName = newServiceName;
         expectedLogs.Push($"Warning : Service {oldServiceName}'s service name has been set to {newServiceName}");
         expectedLogWarnings.Push($"Warning : Service {oldServiceName}'s service name has been set to {newServiceName}");
         service.Url = url;
         service.Url = url;
         expectedLogs.Push($"Information : Service {newServiceName}'s url has been set to {url}");
         service.Notes = notes;
         service.Notes = notes;
         expectedLogs.Push($"Information : Service {newServiceName}'s notes has been set to {notes}");

         databaseCreated.Close();
         expectedLogs.Push($"Warning : User {username} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {username} logged out without saving");
         expectedLogs.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");
         expectedLogs.Push($"Warning : User {username}'s autosave merged");
         expectedLogWarnings.Push($"Warning : User {username}'s autosave merged");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.ServiceName.Should().Be(newServiceName);
         _ = serviceLoaded.Url.Should().Be(url);
         _ = serviceLoaded.Notes.Should().Be(notes);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);
         expectedLogs.Push($"Warning : Service {serviceName} has been removed from User {username}");
         expectedLogWarnings.Push($"Warning : Service {serviceName} has been removed from User {username}");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         // When
         databaseLoaded.Save();
         expectedLogs.Push($"Information : User {username}'s database saved");
         databaseLoaded.Close();
         expectedLogs.Push($"Information : User {username} logged out");
         expectedLogs.Push($"Information : User {username}'s database closed");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);
         expectedLogs.Push($"Warning : Service {serviceName} has been removed from User {username}");
         expectedLogWarnings.Push($"Warning : Service {serviceName} has been removed from User {username}");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"Warning : User {username} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {username} logged out without saving");
         expectedLogs.Push($"Information : User {username}'s database closed");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");
         expectedLogs.Push($"Warning : User {username}'s autosave merged");
         expectedLogWarnings.Push($"Warning : User {username}'s autosave merged");

         // Then
         _ = databaseLoaded.User.Services.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
