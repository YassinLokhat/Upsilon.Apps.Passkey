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
       * User.AddService adds the new service,
       * Then updating the service and saving will save the update in the database file and delete the autosave file,
       * Then Database.Open loads correctly the updated database file with the updated service.
      */
      public void Case01_AddServiceUpdateSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         string url = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService(serviceName);

         // Then
         databaseCreated.User.Services.Count().Should().Be(1);

         // When
         service.Url = url;
         service.Notes = notes;
         databaseCreated.Save();
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         // Then
         databaseLoaded.User.Services.Count().Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         serviceLoaded.ServiceName.Should().Be(serviceName);
         serviceLoaded.Url.Should().Be(url);
         serviceLoaded.Notes.Should().Be(notes);

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
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         string url = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();

         // When
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService(serviceName);

         // Then
         databaseCreated.User.Services.Count().Should().Be(1);

         // When
         service.Url = url;
         service.Notes = notes;
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         // Then
         databaseLoaded.User.Services.Count().Should().Be(1);

         // When
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         serviceLoaded.ServiceName.Should().Be(serviceName);
         serviceLoaded.Url.Should().Be(url);
         serviceLoaded.Notes.Should().Be(notes);

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
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         databaseCreated.User.AddService(serviceName);
         databaseCreated.Save();
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);

         // Then
         databaseLoaded.User.Services.Count().Should().Be(0);

         // When
         databaseLoaded.Save();
         databaseLoaded.Close();
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         // Then
         databaseLoaded.User.Services.Count().Should().Be(0);

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
         string[] passkeys = UnitTestsHelper.GetRandomPasskeys();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         string serviceName = "Service_" + UnitTestsHelper.GetUsername();
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         databaseCreated.User.AddService(serviceName);
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // When
         databaseLoaded.User.DeleteService(serviceLoaded);

         // Then
         databaseLoaded.User.Services.Count().Should().Be(0);

         // When
         databaseLoaded.Save();
         databaseLoaded.Close();
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));

         // Then
         databaseLoaded.User.Services.Count().Should().Be(0);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
