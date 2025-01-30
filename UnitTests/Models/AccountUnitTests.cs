using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Interfaces;

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
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string oldAccountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string newAccountLabel = "new_" + oldAccountLabel;
         string[] oldIdentifiants = UnitTestsHelper.GetRandomStringArray();
         string[] newIdentifiants = UnitTestsHelper.GetRandomStringArray();
         string oldPassword = UnitTestsHelper.GetRandomString();
         string newPassword = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.WarnIfPasswordLeaked;
         Stack<string> expectedLogs = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiants, oldPassword);
         expectedLogs.Push($"Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)}) has been added to Service {service.ServiceName}|False");

         // Then
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(oldIdentifiants);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         expectedLogs.Push($"Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)})'s label has been set to {newAccountLabel}|False");
         account.Identifiants = newIdentifiants;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})|True");
         account.Password = newPassword;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated|True");
         account.Notes = notes;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s notes has been set to {notes}|False");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password update reminder delay has been set to {passwordUpdateReminderDelay}|False");
         account.Options = options;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s options has been set to {options}|False");

         databaseCreated.Save();
         expectedLogs.Push($"User {username}'s database saved|False");
         databaseCreated.Close();
         expectedLogs.Push($"User {username} logged out|False");
         expectedLogs.Push($"User {username}'s database closed|False");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(newAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(newIdentifiants);
         _ = account.Password.Should().Be(newPassword);
         _ = account.Passwords.OrderByDescending(x => x.Key).Select(x => x.Value).Should().BeEquivalentTo([newPassword, oldPassword]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);
         
         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

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
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string oldAccountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string newAccountLabel = "new_" + oldAccountLabel;
         string[] oldIdentifiants = UnitTestsHelper.GetRandomStringArray();
         string[] newIdentifiants = UnitTestsHelper.GetRandomStringArray();
         string oldPassword = UnitTestsHelper.GetRandomString();
         string newPassword = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.WarnIfPasswordLeaked;
         Stack<string> expectedLogs = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiants, oldPassword);
         expectedLogs.Push($"Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)}) has been added to Service {service.ServiceName}|False");

         // Then
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(oldIdentifiants);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         expectedLogs.Push($"Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)})'s label has been set to {newAccountLabel}|False");
         account.Identifiants = newIdentifiants;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})|True");
         account.Password = newPassword;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated|True");
         account.Notes = notes;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s notes has been set to {notes}|False");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password update reminder delay has been set to {passwordUpdateReminderDelay}|False");
         account.Options = options;
         expectedLogs.Push($"Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s options has been set to {options}|False");

         databaseCreated.Close();
         expectedLogs.Push($"User {username} logged out without saving|True");
         expectedLogs.Push($"User {username}'s database closed|False");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");
         expectedLogs.Push($"User {username}'s autosave merged|True");
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(newAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(newIdentifiants);
         _ = account.Password.Should().Be(newPassword);
         _ = account.Passwords.OrderByDescending(x => x.Key).Select(x => x.Value).Should().BeEquivalentTo([newPassword, oldPassword]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

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
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiants, password);
         databaseCreated.Save();
         databaseCreated.Close();
         Stack<string> expectedLogs = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedLogs.Push($"Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}|True");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         // When
         databaseLoaded.Save();
         expectedLogs.Push($"User {username}'s database saved|False");
         databaseLoaded.Close();
         expectedLogs.Push($"User {username} logged out|False");
         expectedLogs.Push($"User {username}'s database closed|False");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

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
         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase databaseCreated = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = databaseCreated.User.AddService("Service_" + UnitTestsHelper.GetUsername());
         string accountLabel = "Account_" + UnitTestsHelper.GetUsername();
         string[] identifiants = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiants, password);
         databaseCreated.Save();
         databaseCreated.Close();
         Stack<string> expectedLogs = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedLogs.Push($"Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}|True");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         // When
         databaseLoaded.Close();
         expectedLogs.Push($"User {username} logged out without saving|True");
         expectedLogs.Push($"User {username}'s database closed|False");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"User {username}'s database opened|False");
         expectedLogs.Push($"User {username} logged in|False");
         expectedLogs.Push($"User {username}'s autosave merged|True");

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
