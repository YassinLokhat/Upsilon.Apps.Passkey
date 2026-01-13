using FluentAssertions;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Models
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
         string[] oldIdentifiers = UnitTestsHelper.GetRandomStringArray();
         string[] newIdentifiers = UnitTestsHelper.GetRandomStringArray();
         string oldPassword = UnitTestsHelper.GetRandomString();
         string newPassword = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.None;
         Stack<string> expectedActivities = new();
         Stack<string> expectedLogWarnings = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiers, oldPassword);
         expectedActivities.Push($"Information : {account} has been added to Service {service.ServiceName}");
         expectedActivities.Push($"Warning : {service}'s {account}'s password has been updated");
         expectedLogWarnings.Push($"Warning : {service}'s {account}'s password has been updated");

         // Then
         databaseCreated.User.HasChanged().Should().BeTrue();
         service.HasChanged().Should().BeTrue();
         account.HasChanged().Should().BeTrue();
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiers.Should().BeEquivalentTo(oldIdentifiers);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         account.Label = newAccountLabel;
         expectedActivities.Push($"Information : {service}'s Account {oldAccountLabel} ({string.Join(", ", oldIdentifiers)})'s label has been set to {newAccountLabel}");
         account.Identifiers = newIdentifiers;
         account.Identifiers = newIdentifiers;
         expectedActivities.Push($"Warning : {service}'s Account {newAccountLabel} ({string.Join(", ", oldIdentifiers)})'s identifiers has been set to ({string.Join(", ", newIdentifiers)})");
         expectedLogWarnings.Push($"Warning : {service}'s Account {newAccountLabel} ({string.Join(", ", oldIdentifiers)})'s identifiers has been set to ({string.Join(", ", newIdentifiers)})");
         account.Password = newPassword;
         account.Password = newPassword;
         expectedActivities.Push($"Warning : {service}'s {account}'s password has been updated");
         expectedLogWarnings.Push($"Warning : {service}'s {account}'s password has been updated");
         account.Notes = notes;
         account.Notes = notes;
         expectedActivities.Push($"Information : {service}'s {account}'s notes has been set to {notes}");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedActivities.Push($"Information : {service}'s {account}'s password update reminder delay has been set to {passwordUpdateReminderDelay}");
         account.Options = options;
         account.Options = options;
         expectedActivities.Push($"Information : {service}'s {account}'s options has been set to {options}");

         // Then
         databaseCreated.User.HasChanged().Should().BeTrue();
         service.HasChanged().Should().BeTrue();
         service.HasChanged().Should().BeTrue();
         account.HasChanged().Should().BeTrue();
         account.HasChanged(nameof(account.Label)).Should().BeTrue();
         account.HasChanged(nameof(account.Identifiers)).Should().BeTrue();
         account.HasChanged(nameof(account.Password)).Should().BeTrue();
         account.HasChanged(nameof(account.Notes)).Should().BeTrue();
         account.HasChanged(nameof(account.PasswordUpdateReminderDelay)).Should().BeTrue();
         account.HasChanged(nameof(account.Options)).Should().BeTrue();

         // When
         databaseCreated.Save();
         expectedActivities.Push($"Information : User {username}'s database saved");
         databaseCreated.Close();
         expectedActivities.Push($"Information : User {username} logged out");
         expectedActivities.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedActivities.Push($"Information : User {username}'s database opened");
         expectedActivities.Push($"Information : User {username} logged in");
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(newAccountLabel);
         _ = account.Identifiers.Should().BeEquivalentTo(newIdentifiers);
         _ = account.Password.Should().Be(newPassword);
         _ = account.Passwords.OrderByDescending(x => x.Key).Select(x => x.Value).Should().BeEquivalentTo([newPassword, oldPassword]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Service.AddAccount adds the new account,
       * Then updating the account without saving will create the autosave file,
       * Then Database.Open with AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile loads correctly the updated database file with the updated account.
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
         string[] oldIdentifiers = UnitTestsHelper.GetRandomStringArray();
         string[] newIdentifiers = UnitTestsHelper.GetRandomStringArray();
         string oldPassword = UnitTestsHelper.GetRandomString();
         string newPassword = UnitTestsHelper.GetRandomString();
         string notes = UnitTestsHelper.GetRandomString();
         int passwordUpdateReminderDelay = UnitTestsHelper.GetRandomInt(12);
         AccountOption options = AccountOption.None;
         Stack<string> expectedActivities = new();
         Stack<string> expectedLogWarnings = new();

         // When
         IAccount account = service.AddAccount(oldAccountLabel, oldIdentifiers, oldPassword);
         expectedActivities.Push($"Information : {account} has been added to Service {service.ServiceName}");
         expectedActivities.Push($"Warning : {service}'s {account}'s password has been updated");
         expectedLogWarnings.Push($"Warning : {service}'s {account}'s password has been updated");

         // Then
         _ = service.Accounts.Length.Should().Be(1);
         _ = account.Label.Should().Be(oldAccountLabel);
         _ = account.Identifiers.Should().BeEquivalentTo(oldIdentifiers);
         _ = account.Password.Should().Be(oldPassword);
         _ = account.Passwords.Values.Should().BeEquivalentTo([oldPassword]);

         // When
         account.Label = newAccountLabel;
         account.Label = newAccountLabel;
         expectedActivities.Push($"Information : {service}'s Account {oldAccountLabel} ({string.Join(", ", oldIdentifiers)})'s label has been set to {newAccountLabel}");
         account.Identifiers = newIdentifiers;
         account.Identifiers = newIdentifiers;
         expectedActivities.Push($"Warning : {service}'s Account {newAccountLabel} ({string.Join(", ", oldIdentifiers)})'s identifiers has been set to ({string.Join(", ", newIdentifiers)})");
         expectedLogWarnings.Push($"Warning : {service}'s Account {newAccountLabel} ({string.Join(", ", oldIdentifiers)})'s identifiers has been set to ({string.Join(", ", newIdentifiers)})");
         account.Password = newPassword;
         account.Password = newPassword;
         expectedActivities.Push($"Warning : {service}'s {account}'s password has been updated");
         expectedLogWarnings.Push($"Warning : {service}'s {account}'s password has been updated");
         account.Notes = notes;
         account.Notes = notes;
         expectedActivities.Push($"Information : {service}'s {account}'s notes has been set to {notes}");
         account.PasswordUpdateReminderDelay = passwordUpdateReminderDelay;
         expectedActivities.Push($"Information : {service}'s {account}'s password update reminder delay has been set to {passwordUpdateReminderDelay}");
         account.Options = options;
         account.Options = options;
         expectedActivities.Push($"Information : {service}'s {account}'s options has been set to {options}");

         databaseCreated.Close();
         expectedActivities.Push($"Warning : User {username} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {username} logged out without saving");
         expectedActivities.Push($"Information : User {username}'s database closed");

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile);
         expectedActivities.Push($"Information : User {username}'s database opened");
         expectedActivities.Push($"Information : User {username} logged in");
         expectedActivities.Push($"Warning : User {username}'s autosave merged and saved");
         expectedLogWarnings.Push($"Warning : User {username}'s autosave merged and saved");
         IService serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(1);

         // When
         _ = serviceLoaded.Accounts.First();

         // Then
         _ = account.Label.Should().Be(newAccountLabel);
         _ = account.Identifiers.Should().BeEquivalentTo(newIdentifiers);
         _ = account.Password.Should().Be(newPassword);
         _ = account.Passwords.OrderByDescending(x => x.Key).Select(x => x.Value).Should().BeEquivalentTo([newPassword, oldPassword]);
         _ = account.Notes.Should().Be(notes);
         _ = account.PasswordUpdateReminderDelay.Should().Be(passwordUpdateReminderDelay);
         _ = account.Options.Should().Be(options);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

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
         string[] identifiers = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiers, password);
         databaseCreated.Save();
         databaseCreated.Close();
         Stack<string> expectedActivities = new();
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedActivities.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiers)}) has been removed from Service {service.ServiceName}");
         expectedLogWarnings.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiers)}) has been removed from Service {service.ServiceName}");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         // When
         databaseLoaded.Save();
         expectedActivities.Push($"Information : User {username}'s database saved");
         databaseLoaded.Close();
         expectedActivities.Push($"Information : User {username} logged out");
         expectedActivities.Push($"Information : User {username}'s database closed");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         expectedActivities.Push($"Information : User {username}'s database opened");
         expectedActivities.Push($"Information : User {username} logged in");

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Service.DeleteAccount adeletes the account,
       * Then Database.Open with AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile loads correctly the updated database file with the updated account.
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
         string[] identifiers = UnitTestsHelper.GetRandomStringArray();
         string password = UnitTestsHelper.GetRandomString();
         _ = service.AddAccount(accountLabel, identifiers, password);
         databaseCreated.Save();
         databaseCreated.Close();
         Stack<string> expectedActivities = new();
         Stack<string> expectedLogWarnings = new();

         IDatabase databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         IService serviceLoaded = databaseLoaded.User.Services.First();
         IAccount accountLoaded = serviceLoaded.Accounts.First();

         // When
         serviceLoaded.DeleteAccount(accountLoaded);
         expectedActivities.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiers)}) has been removed from Service {service.ServiceName}");
         expectedLogWarnings.Push($"Warning : Account {accountLabel} ({string.Join(", ", identifiers)}) has been removed from Service {service.ServiceName}");

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         // When
         databaseLoaded.Close();
         expectedActivities.Push($"Warning : User {username} logged out without saving");
         expectedLogWarnings.Push($"Warning : User {username} logged out without saving");
         expectedActivities.Push($"Information : User {username}'s database closed");

         databaseLoaded = UnitTestsHelper.OpenTestDatabase(passkeys, out _, AutoSaveMergeBehavior.MergeAndSaveThenRemoveAutoSaveFile);
         expectedActivities.Push($"Information : User {username}'s database opened");
         expectedActivities.Push($"Information : User {username} logged in");
         expectedActivities.Push($"Warning : User {username}'s autosave merged and saved");
         expectedLogWarnings.Push($"Warning : User {username}'s autosave merged and saved");

         serviceLoaded = databaseLoaded.User.Services.First();

         // Then
         _ = serviceLoaded.Accounts.Length.Should().Be(0);

         UnitTestsHelper.LastActivitiesShouldMatch(databaseLoaded, [.. expectedActivities]);
         UnitTestsHelper.LastActivityWarningsShouldMatch(databaseLoaded, [.. expectedLogWarnings]);

         // Finaly
         databaseLoaded.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
