using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Upsilon.Apps.PassKey.Interfaces;
using Upsilon.Apps.PassKey.Interfaces.Enums;

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
         Stack<string> expectedLogWarnings = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiants, oldPassword);
         expectedLogs.Push($"Information : Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)}) has been added to Service {service.ServiceName}");

         // Then
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(oldIdentifiants);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         expectedLogs.Push($"Information : Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)})'s label has been set to {newAccountLabel}");
         account.Identifiants = newIdentifiants;
         expectedLogs.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})");
         expectedLogWarnings.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})");
         account.Password = newPassword;
         expectedLogs.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated");
         expectedLogWarnings.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated");
         account.Notes = notes;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s notes has been set to {notes}");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password update reminder delay has been set to {passwordUpdateReminderDelay}");
         account.Options = options;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s options has been set to {options}");

         databaseCreated.Save();
         expectedLogs.Push($"Information : User {username}'s database saved");
         databaseCreated.Close();
         expectedLogs.Push($"Information : User {username} logged out");
         expectedLogs.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");
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
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiants, oldPassword);
         expectedLogs.Push($"Information : Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)}) has been added to Service {service.ServiceName}");

         // Then
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiants.Should().BeEquivalentTo(oldIdentifiants);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         expectedLogs.Push($"Information : Account {oldAccountLabel} ({string.Join(", ", oldIdentifiants)})'s label has been set to {newAccountLabel}");
         account.Identifiants = newIdentifiants;
         expectedLogs.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})");
         expectedLogWarnings.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", oldIdentifiants)})'s identifiants has been set to ({string.Join(", ", newIdentifiants)})");
         account.Password = newPassword;
         expectedLogs.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated");
         expectedLogWarnings.Push($"Warning : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password has been updated");
         account.Notes = notes;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s notes has been set to {notes}");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s password update reminder delay has been set to {passwordUpdateReminderDelay}");
         account.Options = options;
         expectedLogs.Push($"Information : Account {newAccountLabel} ({string.Join(", ", newIdentifiants)})'s options has been set to {options}");

         databaseCreated.Close();
         expectedLogs.Push($"Warning : User {username} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {username} logged out without saving");
         expectedLogs.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeThenRemoveAutoSaveFile);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");
         expectedLogs.Push($"Warning : User {username}'s autosave merged");
         expectedLogWarnings.Push($"Warning : User {username}'s autosave merged");
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
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedLogs.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}");
         expectedLogWarnings.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         // When
         databaseLoaded.Save();
         expectedLogs.Push($"Information : User {username}'s database saved");
         databaseLoaded.Close();
         expectedLogs.Push($"Information : User {username} logged out");
         expectedLogs.Push($"Information : User {username}'s database closed");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedLogs.Push($"Information : User {username}'s database opened");
         expectedLogs.Push($"Information : User {username} logged in");

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedLogs.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}");
         expectedLogWarnings.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiants)}) has been removed from Service {service.ServiceName}");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

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

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastLogsShouldMatch(databaseLoaded, [.. expectedLogs]);
         UnitTestsHelper.LastLogWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
