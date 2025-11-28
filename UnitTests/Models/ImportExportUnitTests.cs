using ABI.System;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Upsilon.Apps.Passkey.Core.Public.Enums;
using Upsilon.Apps.Passkey.Core.Public.Interfaces;
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
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath("missing_import.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because import file is not accessible");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

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
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"{username}/import.txt", createIfNotExists: true);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because '.txt' extention type is not handled");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

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
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_noData.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because there is no data to import");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

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
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();
         database.User.AddService("Service1");

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because service 'Service1' already exists");

         // Then
         database.User.Services.Length.Should().Be(1);
         database.User.Services[0].Url.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

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
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_blanckService.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because service name or account identifiant cannot be blank");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
    
      [TestMethod]
      public void Case06_ImportBlanckIdentifiant()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_blanckIdentifiant.json");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because service name or account identifiant cannot be blank");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
    
      [TestMethod]
      public void Case07_ImportCSV_OK()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath("import.csv");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");

         expectedLogs.Push($"Information : Service Service0 has been added to User {username}");
         expectedLogs.Push($"Information : Service Service0's url has been set to www.service0.xyz");
         expectedLogs.Push($"Information : Service Service0's notes has been set to Service0's notes");

         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz) has been added to Service Service0");
         expectedLogs.Push($"Warning : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s notes has been set to Service0's Account0's notes");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz) has been added to Service Service0");
         expectedLogs.Push($"Warning : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s notes has been set to Service0's Account1's notes");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Service Service1 has been added to User {username}");
         expectedLogs.Push($"Information : Service Service1's url has been set to www.service1.xyz");
         expectedLogs.Push($"Information : Service Service1's notes has been set to Service1's notes");

         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz) has been added to Service Service1");
         expectedLogs.Push($"Warning : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s notes has been set to Service1's Account0's notes");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz) has been added to Service Service1");
         expectedLogs.Push($"Warning : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s notes has been set to Service1's Account1's notes");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Warning : Import completed successfully");
         expectedLogs.Push($"Information : User {username}'s database saved");

         // Then
         database.User.Services.Length.Should().Be(2);

         database.User.Services[0].ServiceName.Should().Be("Service0");
         database.User.Services[0].Url.Should().Be("www.service0.xyz");
         database.User.Services[0].Notes.Should().Be("Service0's notes");

         database.User.Services[0].Accounts.Length.Should().Be(2);

         database.User.Services[0].Accounts[0].Label.Should().Be("Account0");
         database.User.Services[0].Accounts[0].Identifiants.Should().BeEquivalentTo(new[] { "account0@service0.xyz", "account0_backup@service0.xyz" });
         database.User.Services[0].Accounts[0].Password.Should().Be("0000");
         database.User.Services[0].Accounts[0].Notes.Should().Be("Service0's Account0's notes");
         database.User.Services[0].Accounts[0].Options.Should().Be(AccountOption.None);
         database.User.Services[0].Accounts[0].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[0].Accounts[1].Label.Should().Be("Account1");
         database.User.Services[0].Accounts[1].Identifiants.Should().BeEquivalentTo(new[] { "account1@service0.xyz", "account1_backup@service0.xyz" });
         database.User.Services[0].Accounts[1].Password.Should().Be("1111");
         database.User.Services[0].Accounts[1].Notes.Should().Be("Service0's Account1's notes");
         database.User.Services[0].Accounts[1].Options.Should().Be(AccountOption.None);
         database.User.Services[0].Accounts[1].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[1].ServiceName.Should().Be("Service1");
         database.User.Services[1].Url.Should().Be("www.service1.xyz");
         database.User.Services[1].Notes.Should().Be("Service1's notes");

         database.User.Services[1].Accounts.Length.Should().Be(2);

         database.User.Services[1].Accounts[0].Label.Should().Be("Account0");
         database.User.Services[1].Accounts[0].Identifiants.Should().BeEquivalentTo(new[] { "account0@service1.xyz", "account0_backup@service1.xyz" });
         database.User.Services[1].Accounts[0].Password.Should().Be("AAAA");
         database.User.Services[1].Accounts[0].Notes.Should().Be("Service1's Account0's notes");
         database.User.Services[1].Accounts[0].Options.Should().Be(AccountOption.None);
         database.User.Services[1].Accounts[0].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[1].Accounts[1].Label.Should().Be("Account1");
         database.User.Services[1].Accounts[1].Identifiants.Should().BeEquivalentTo(new[] { "account1@service1.xyz", "account1_backup@service1.xyz" });
         database.User.Services[1].Accounts[1].Password.Should().Be("BBBB");
         database.User.Services[1].Accounts[1].Notes.Should().Be("Service1's Account1's notes");
         database.User.Services[1].Accounts[1].Options.Should().Be(AccountOption.None);
         database.User.Services[1].Accounts[1].PasswordUpdateReminderDelay.Should().Be(3);

         // When
         database.ExportToFile(exportFile);
         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedLogs.Push($"Warning : Export completed successfully");

         // Then
         File.ReadAllText(importFile).Should().Be(File.ReadAllText(exportFile));

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case08_ImportCSV_MissingHeader()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_MissingHearder.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because the CSV headers should be : 'ServiceName', 'ServiceUrl', 'ServiceNotes', 'AccountLabel', 'Identifiants', 'Password', 'AccountNotes', 'AccountOptions', 'PasswordUpdateReminderDelay'");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case09_ImportCSV_MissingCollumn()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_MissingCollumn.csv");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because the CSV data format is incorrect");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case10_ImportJson_OK()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath("import.json");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");

         expectedLogs.Push($"Information : Service Service0 has been added to User {username}");
         expectedLogs.Push($"Information : Service Service0's url has been set to www.service0.xyz");
         expectedLogs.Push($"Information : Service Service0's notes has been set to Service0's notes");

         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz) has been added to Service Service0");
         expectedLogs.Push($"Warning : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s notes has been set to Service0's Account0's notes");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account0 (account0@service0.xyz, account0_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz) has been added to Service Service0");
         expectedLogs.Push($"Warning : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s notes has been set to Service0's Account1's notes");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account1 (account1@service0.xyz, account1_backup@service0.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Service Service1 has been added to User {username}");
         expectedLogs.Push($"Information : Service Service1's url has been set to www.service1.xyz");
         expectedLogs.Push($"Information : Service Service1's notes has been set to Service1's notes");

         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz) has been added to Service Service1");
         expectedLogs.Push($"Warning : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s notes has been set to Service1's Account0's notes");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account0 (account0@service1.xyz, account0_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz) has been added to Service Service1");
         expectedLogs.Push($"Warning : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password has been updated");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s notes has been set to Service1's Account1's notes");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s options has been set to None");
         expectedLogs.Push($"Information : Account Account1 (account1@service1.xyz, account1_backup@service1.xyz)'s password update reminder delay has been set to 3");

         expectedLogs.Push($"Warning : Import completed successfully");
         expectedLogs.Push($"Information : User {username}'s database saved");

         // Then
         database.User.Services.Length.Should().Be(2);

         database.User.Services[0].ServiceName.Should().Be("Service0");
         database.User.Services[0].Url.Should().Be("www.service0.xyz");
         database.User.Services[0].Notes.Should().Be("Service0's notes");

         database.User.Services[0].Accounts.Length.Should().Be(2);

         database.User.Services[0].Accounts[0].Label.Should().Be("Account0");
         database.User.Services[0].Accounts[0].Identifiants.Should().BeEquivalentTo(new[] { "account0@service0.xyz", "account0_backup@service0.xyz" });
         database.User.Services[0].Accounts[0].Password.Should().Be("0000");
         database.User.Services[0].Accounts[0].Notes.Should().Be("Service0's Account0's notes");
         database.User.Services[0].Accounts[0].Options.Should().Be(AccountOption.None);
         database.User.Services[0].Accounts[0].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[0].Accounts[1].Label.Should().Be("Account1");
         database.User.Services[0].Accounts[1].Identifiants.Should().BeEquivalentTo(new[] { "account1@service0.xyz", "account1_backup@service0.xyz" });
         database.User.Services[0].Accounts[1].Password.Should().Be("1111");
         database.User.Services[0].Accounts[1].Notes.Should().Be("Service0's Account1's notes");
         database.User.Services[0].Accounts[1].Options.Should().Be(AccountOption.None);
         database.User.Services[0].Accounts[1].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[1].ServiceName.Should().Be("Service1");
         database.User.Services[1].Url.Should().Be("www.service1.xyz");
         database.User.Services[1].Notes.Should().Be("Service1's notes");

         database.User.Services[1].Accounts.Length.Should().Be(2);

         database.User.Services[1].Accounts[0].Label.Should().Be("Account0");
         database.User.Services[1].Accounts[0].Identifiants.Should().BeEquivalentTo(new[] { "account0@service1.xyz", "account0_backup@service1.xyz" });
         database.User.Services[1].Accounts[0].Password.Should().Be("AAAA");
         database.User.Services[1].Accounts[0].Notes.Should().Be("Service1's Account0's notes");
         database.User.Services[1].Accounts[0].Options.Should().Be(AccountOption.None);
         database.User.Services[1].Accounts[0].PasswordUpdateReminderDelay.Should().Be(3);

         database.User.Services[1].Accounts[1].Label.Should().Be("Account1");
         database.User.Services[1].Accounts[1].Identifiants.Should().BeEquivalentTo(new[] { "account1@service1.xyz", "account1_backup@service1.xyz" });
         database.User.Services[1].Accounts[1].Password.Should().Be("BBBB");
         database.User.Services[1].Accounts[1].Notes.Should().Be("Service1's Account1's notes");
         database.User.Services[1].Accounts[1].Options.Should().Be(AccountOption.None);
         database.User.Services[1].Accounts[1].PasswordUpdateReminderDelay.Should().Be(3);

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      public void Case11_ImportJson_WrongFormat()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import_WrongFormat.json");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();

         // When
         database.ImportFromFile(importFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Importing data from file : '{importFile}'");
         expectedLogs.Push($"Warning : Import failed because import file deserialization failed");

         // Then
         database.User.Services.Should().BeEmpty();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }


      [TestMethod]
      public void Case12_Export_FileAlreadyExists()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.json");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.json", createIfNotExists: true);
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();
         database.ImportFromFile(importFile);

         // When
         database.ExportToFile(exportFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedLogs.Push($"Warning : Export failed because export file already exists");

         // Then
         File.Exists(exportFile).Should().BeTrue();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }


      [TestMethod]
      public void Case13_Export_FileAlreadyExists()
      {
         // Given
         UnitTestsHelper.ClearTestEnvironment();

         string username = UnitTestsHelper.GetUsername();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         string databaseFile = UnitTestsHelper.ComputeDatabaseFilePath();
         string autoSaveFile = UnitTestsHelper.ComputeAutoSaveFilePath();
         string logFile = UnitTestsHelper.ComputeLogFilePath();
         string importFile = UnitTestsHelper.GetTestFilePath($"import.json");
         string exportFile = UnitTestsHelper.GetTestFilePath($"{username}/export.txt");
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Stack<string> expectedLogs = new();
         database.ImportFromFile(importFile);

         // When
         database.ExportToFile(exportFile);

         expectedLogs.Push($"Information : User {username}'s database saved");
         expectedLogs.Push($"Warning : Exporting data to file : '{exportFile}'");
         expectedLogs.Push($"Warning : Export failed because '.txt' extention type is not handled");

         // Then
         File.Exists(exportFile).Should().BeFalse();

         UnitTestsHelper.LastLogsShouldMatch(database, [.. expectedLogs]);

         // Finaly
         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
