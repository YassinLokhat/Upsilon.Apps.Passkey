using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests
{
   /// <summary>
   /// Manual vault fixture generator (not part of the automated test suite).
   /// </summary>
   [TestClass]
   public sealed class TestDatabaseGenerator
   {
      // Intentionally not a [TestMethod]: run manually when regenerating large fixtures.
      public void GenerateNewDatabase()
      {
         UnitTestsHelper.ClearTestEnvironment("_");

         IDatabase database = UnitTestsHelper.CreateTestDatabase(["a", "b"], "_");
         IUser user = database.User;
         user.Settings.LogoutTimeout = 0;
         user.Settings.CleaningClipboardTimeout = 5;
         user.Settings.ShowPasswordDelay = 0;
         user.Settings.NumberOfOldPasswordToKeep = 0;
         user.Settings.NumberOfMonthActivitiesToKeep = 0;
         user.Settings.Theme = "System";
         user.Settings.Language = "System";
         user.Settings.AlertsToNotify = new AlertKindList(AlertKinds.DefaultNotify);
         string logFile = database.DatabaseFile.Replace(".pku", ".log");
         File.WriteAllText(logFile, string.Empty);

         for (int i = 0; i < 25; i++)
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
                        identifiers: UnitTestsHelper.GetRandomIdentifierArray(random / 2));
                     break;
                  case 2:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomIdentifierArray(random / 2),
                        password: password);
                     break;
                  case 3:
                     account = service.AddAccount(identifiers: UnitTestsHelper.GetRandomIdentifierArray(random / 2));
                     break;
                  default:
                     account = service.AddAccount(label: $"Account{j}",
                        identifiers: UnitTestsHelper.GetRandomIdentifierArray(random / 2),
                        password: password);
                     break;
               }

               random = UnitTestsHelper.GetRandomInt(100);
               account.Notes = random % 10 == 0 ? $"Service{i}'s Account{j} notes : \n{UnitTestsHelper.GetRandomString(min: 10, max: 150)}" : "";
               account.PasswordUpdateReminderDelay = 0;
               account.Options = (!string.IsNullOrEmpty(account.Password) && random % 2 == 0) ? AccountOption.WarnIfPasswordLeaked : AccountOption.None;
               File.AppendAllText(logFile, "#");
            }
            File.AppendAllText(logFile, "\n");
         }

         IService s10 = database.User.Services.First(x => x.ServiceName.StartsWith("Service10 "));
         s10.Accounts.First().Password = "test";
         s10.Accounts.First().Options = AccountOption.WarnIfPasswordLeaked | AccountOption.WarnIfDuplicatedPassword;

         IService s2 = database.User.Services.First(x => x.ServiceName.StartsWith("Service2 "));
         s2.Accounts.First().Password = "test";
         s2.Accounts.First().Options = AccountOption.WarnIfPasswordLeaked;

         IService s20 = database.User.Services.First(x => x.ServiceName.StartsWith("Service20 "));
         s20.Accounts.First().Password = "test";
         s20.Accounts.First().Options = AccountOption.WarnIfDuplicatedPassword;

         database.Save();

         string exportFile = database.DatabaseFile.Replace(".pku", ".json");

         if (database.ExportToFile(exportFile) == ImportExportError.None)
         {
            database.Delete();

            database = UnitTestsHelper.CreateTestDatabase(["a", "b"], "_");
            database.ImportFromFile(exportFile);
         }

         database.Close();
      }
   }
}
