using ABI.System;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public class ImportExportUnitTests
   {
      [TestMethod]
      public void Case01_Import_MissingFile()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath("missing_import.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because import file is not accessible");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case02_Import_WrongExtention()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"{username}/import.txt", createIfNotExists: true);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because '.txt' extension type is not handled");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case03_Import_NoData()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_noData.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because there is no data to import");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case04_Import_ServiceAlreadyExists()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();
         database.User.AddService("Service1");

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Information : User {username}'s database saved");
         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because service 'Service1' already exists");

         // Then
         database.User.Services.Count().Should().Be(1);
         database.User.Services.ElementAt(0).Url.Should().BeNull();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case05_ImportBlanckService()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_blanckService.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because service name cannot be blank");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case06_ImportCSV_OK()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath("import.csv");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");

         expectedActivities.Push($"Information : Service Service0 has been added to User {username}");
         expectedActivities.Push($"Information : Service Service0's url has been set to http://service0.xyz");
         expectedActivities.Push($"Information : Service Service0's notes has been set to Service0's notes");

         expectedActivities.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz) has been added to Service Service0");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s notes has been set to Service0's Account0's notes");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz) has been added to Service Service0");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s notes has been set to Service0's Account1's notes");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Service Service1 has been added to User {username}");
         expectedActivities.Push($"Information : Service Service1's url has been set to http://service1.xyz");
         expectedActivities.Push($"Information : Service Service1's notes has been set to Service1's notes");

         expectedActivities.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz) has been added to Service Service1");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s notes has been set to Service1's Account0's notes");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz) has been added to Service Service1");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s notes has been set to Service1's Account1's notes");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Warning : Import completed successfully");
         expectedActivities.Push($"Information : User {username}'s database saved");

         // Then
         database.User.Services.Count().Should().Be(2);

         database.User.Services.ElementAt(0).ServiceName.Should().Be("Service0");
         database.User.Services.ElementAt(0).Url.OriginalString.Should().Be("http://service0.xyz");
         database.User.Services.ElementAt(0).Notes.Should().Be("Service0's notes");

         database.User.Services.ElementAt(0).Accounts.Count().Should().Be(2);

         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Label.Should().Be("Account0");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Identifiers.Should().BeEquivalentTo(new[] { "account0@service0.xyz", "account0_backup@service0.xyz" });
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Password.Should().Be("0000");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Notes.Should().Be("Service0's Account0's notes");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Label.Should().Be("Account1");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Identifiers.Should().BeEquivalentTo(new[] { "account1@service0.xyz", "account1_backup@service0.xyz" });
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Password.Should().Be("1111");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Notes.Should().Be("Service0's Account1's notes");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(1).ServiceName.Should().Be("Service1");
         database.User.Services.ElementAt(1).Url.OriginalString.Should().Be("http://service1.xyz");
         database.User.Services.ElementAt(1).Notes.Should().Be("Service1's notes");

         database.User.Services.ElementAt(1).Accounts.Count().Should().Be(2);

         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Label.Should().Be("Account0");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Identifiers.Should().BeEquivalentTo(new[] { "account0@service1.xyz", "account0_backup@service1.xyz" });
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Password.Should().Be("AAAA");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Notes.Should().Be("Service1's Account0's notes");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Label.Should().Be("Account1");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Identifiers.Should().BeEquivalentTo(new[] { "account1@service1.xyz", "account1_backup@service1.xyz" });
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Password.Should().Be("BBBB");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Notes.Should().Be("Service1's Account1's notes");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).PasswordUpdateReminderDelay.Should().Be(3);

         // When
         database.ExportToFile(exportFile);
         expectedActivities.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedActivities.Push($"Warning : Export completed successfully");

         // Then
         File.ReadAllText(importFile).Replace("\r", "").Should().Be(File.ReadAllText(exportFile).Replace("\r", ""));

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case07_ImportCSV_MissingHeader()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_MissingHearder.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because the CSV headers should be : 'ServiceName', 'ServiceUrl', 'ServiceNotes', 'AccountLabel', 'Identifiers', 'Password', 'AccountNotes', 'AccountOptions', 'PasswordUpdateReminderDelay'");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case08_ImportCSV_MissingCollumn()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_MissingCollumn.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because the CSV data format is incorrect");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case09_ImportJson_OK()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath("import.json");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");

         expectedActivities.Push($"Information : Service Service0 has been added to User {username}");
         expectedActivities.Push($"Information : Service Service0's url has been set to http://service0.xyz");
         expectedActivities.Push($"Information : Service Service0's notes has been set to Service0's notes");

         expectedActivities.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz) has been added to Service Service0");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s notes has been set to Service0's Account0's notes");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service0's Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz) has been added to Service Service0");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s notes has been set to Service0's Account1's notes");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service0's Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Service Service1 has been added to User {username}");
         expectedActivities.Push($"Information : Service Service1's url has been set to http://service1.xyz");
         expectedActivities.Push($"Information : Service Service1's notes has been set to Service1's notes");

         expectedActivities.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz) has been added to Service Service1");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s notes has been set to Service1's Account0's notes");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service1's Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz) has been added to Service Service1");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password has been updated");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s notes has been set to Service1's Account1's notes");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s options has been set to None");
         expectedActivities.Push($"Information : Service Service1's Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedActivities.Push($"Warning : Import completed successfully");
         expectedActivities.Push($"Information : User {username}'s database saved");

         // Then
         database.User.Services.Count().Should().Be(2);

         database.User.Services.ElementAt(0).ServiceName.Should().Be("Service0");
         database.User.Services.ElementAt(0).Url.OriginalString.Should().Be("http://service0.xyz");
         database.User.Services.ElementAt(0).Notes.Should().Be("Service0's notes");

         database.User.Services.ElementAt(0).Accounts.Count().Should().Be(2);

         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Label.Should().Be("Account0");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Identifiers.Should().BeEquivalentTo(new[] { "account0@service0.xyz", "account0_backup@service0.xyz" });
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Password.Should().Be("0000");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Notes.Should().Be("Service0's Account0's notes");
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(0).Accounts.ElementAt(0).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Label.Should().Be("Account1");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Identifiers.Should().BeEquivalentTo(new[] { "account1@service0.xyz", "account1_backup@service0.xyz" });
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Password.Should().Be("1111");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Notes.Should().Be("Service0's Account1's notes");
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(0).Accounts.ElementAt(1).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(1).ServiceName.Should().Be("Service1");
         database.User.Services.ElementAt(1).Url.OriginalString.Should().Be("http://service1.xyz");
         database.User.Services.ElementAt(1).Notes.Should().Be("Service1's notes");

         database.User.Services.ElementAt(1).Accounts.Count().Should().Be(2);

         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Label.Should().Be("Account0");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Identifiers.Should().BeEquivalentTo(new[] { "account0@service1.xyz", "account0_backup@service1.xyz" });
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Password.Should().Be("AAAA");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Notes.Should().Be("Service1's Account0's notes");
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(1).Accounts.ElementAt(0).PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Label.Should().Be("Account1");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Identifiers.Should().BeEquivalentTo(new[] { "account1@service1.xyz", "account1_backup@service1.xyz" });
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Password.Should().Be("BBBB");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Notes.Should().Be("Service1's Account1's notes");
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).Options.Should().Be(AccountOption.None);
         database.User.Services.ElementAt(1).Accounts.ElementAt(1).PasswordUpdateReminderDelay.Should().Be(3);

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case10_ImportJson_WrongFormat()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_WrongFormat.json");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();

         // When
         database.ImportFromFile(importFile);

         expectedActivities.Push($"Warning : Importing data from file : '{importFile}'");
         expectedActivities.Push($"Warning : Import failed because import file deserialization failed");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }


      [TestMethod]
      public void Case11_Export_FileAlreadyExists()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.json");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.json", createIfNotExists: true);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();
         database.ImportFromFile(importFile);

         // When
         database.ExportToFile(exportFile);

         expectedActivities.Push($"Information : User {username}'s database saved");
         expectedActivities.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedActivities.Push($"Warning : Export failed because export file already exists");

         // Then
         File.Exists(exportFile).Should().BeTrue();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }


      [TestMethod]
      public void Case12_Export_FileExtensionNotHandled()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.json");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.txt");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedActivities = new();
         database.ImportFromFile(importFile);

         // When
         database.ExportToFile(exportFile);

         expectedActivities.Push($"Information : User {username}'s database saved");
         expectedActivities.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedActivities.Push($"Warning : Export failed because '.txt' extension type is not handled");

         // Then
         File.Exists(exportFile).Should().BeFalse();

         UnitTestsHelper.LastActivitiesShouldMatch(database, [.. expectedActivities]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Data exported to JSON can be re-imported into a fresh database and yields
       * an equivalent set of services and accounts (a structural round-trip). A
       * plain file comparison is not usable here because the JSON carries the
       * per-item ItemId and password timestamps, which are regenerated on import.
      */
      public void Case13_ImportExportJson_RoundTrip()
      {
         // Given
         string username = UnitTestsHelper.GetUsername();
         string roundTripUsername = $"{username}_roundtrip";
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string importFile = UnitTestsHelper.GetTestFilePath("import.json");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export_roundtrip.json");

         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.ClearTestEnvironment(roundTripUsername);

         IDatabase source = UnitTestsHelper.CreateTestDatabase(passkeys);

         // When (import into the source database, then export it back to JSON)
         source.ImportFromFile(importFile).Should().BeTrue();
         source.ExportToFile(exportFile).Should().BeTrue();

         // Then (the exported file can be re-imported into a fresh database)
         IDatabase roundTripped = UnitTestsHelper.CreateTestDatabase(passkeys, roundTripUsername);
         roundTripped.ImportFromFile(exportFile).Should().BeTrue();

         // Then (both databases hold an equivalent set of services and accounts)
         _project(source).Should().BeEquivalentTo(_project(roundTripped));

         // Finaly
         source.Close();
         roundTripped.Close();
         UnitTestsHelper.ClearTestEnvironment();
         UnitTestsHelper.ClearTestEnvironment(roundTripUsername);
      }

      // Projects a database's services/accounts onto the persisted fields only,
      // excluding the regenerated ItemId and password timestamps, so two imports
      // of the same data compare as equivalent.
      private static object _project(IDatabase database)
         => database.User.Services.Select(service => new
         {
            service.ServiceName,
            Url = service.Url?.OriginalString,
            service.Notes,
            Accounts = service.Accounts.Select(account => new
            {
               account.Label,
               Identifiers = account.Identifiers.ToArray(),
               account.Password,
               account.Notes,
               account.Options,
               account.PasswordUpdateReminderDelay,
            }).ToArray(),
         }).ToArray();
   }
}
