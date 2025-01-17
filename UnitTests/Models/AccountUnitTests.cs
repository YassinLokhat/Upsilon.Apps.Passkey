using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Interfaces;
using Upsilon.Apps.PassKey.Core.Enums;

namespace Upsilon.Apps.PassKey.UnitTests.Models
{
   [TestClass]
   public sealed class AccountUnitTests
   {
      [TestMethod]
      /*
       * Service.AddAccount adds the new account,
       * Then updating the account and saving will save the update in the database file and delete the autosave file,
       * Then Database.Open loads correctly the updated database file with the updated account.
      */
      public void Case01_AddAccountUpdateSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.WarnIfPasswordLeaked;

         // When
         IAccount account = service.AddAccount(accountLabel, identifiants, password);

         // Then
         _ = service.Accounts.Count().Should().Be(1);
         _ = account.Label.Should().Be(accountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(identifiants);
         _ = account.Password.Should().Be(password);
         _ = account.Passwords.Values.Should().BeEquivalentTo([password]);

         // When
         account.Notes = notes;
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         account.Options = options;
         databaseCreated.Save();
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(accountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(identifiants);
         _ = account.Password.Should().Be(password);
         _ = account.Passwords.Values.Should().BeEquivalentTo([password]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Service.AddAccount adds the new account,
       * Then updating the account without saving will create the autosave file,
       * Then Database.Open with AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile loads correctly the updated database file with the updated account.
      */
      public void Case02_AddAccountUpdateAutoSave()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.WarnIfPasswordLeaked;

         // When
         IAccount account = service.AddAccount(accountLabel, identifiants, password);

         // Then
         _ = service.Accounts.Count().Should().Be(1);
         _ = account.Label.Should().Be(accountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(identifiants);
         _ = account.Password.Should().Be(password);
         _ = account.Passwords.Values.Should().BeEquivalentTo([password]);

         // When
         account.Notes = notes;
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         account.Options = options;
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(accountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(identifiants);
         _ = account.Password.Should().Be(password);
         _ = account.Passwords.Values.Should().BeEquivalentTo([password]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Service.DeleteAccount deletes the account,
       * Then Database.Open loads correctly the updated database file with the updated account.
      */
      public void Case03_DeleteAccountUpdateSaved()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiants, password);
         databaseCreated.Save();
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(0);

         // When
         databaseLoaded.Save();
         databaseLoaded.Close();
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(0);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Service.DeleteAccount adeletes the account,
       * Then Database.Open with AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile loads correctly the updated database file with the updated account.
      */
      public void Case04_DeleteAccountUpdateAutoSave()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         if (databaseCreated.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiants, password);
         databaseCreated.Save();
         databaseCreated.Close();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(0);

         // When
         databaseLoaded.Close();
         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         if (databaseLoaded.User == null) throw new NullReferenceException(nameof(databaseCreated.User));
         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Count().Should().Be(0);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
