using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class SettingsUnitTests
   {
      [TestMethod]
      /*
       * ShowPasswordDelay is persisted through Save / Open like the other settings.
      */
      public void Case01_ShowPasswordDelay_RoundTrip()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);

         database.User!.Settings.ShowPasswordDelay = 12;
         database.Save();
         database.Close();

         IDatabase reopened = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = reopened.User!.Settings.ShowPasswordDelay.Should().Be(12);

         reopened.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Lowering NumberOfOldPasswordToKeep immediately prunes existing history
       * rather than waiting for the next password change.
      */
      public void Case02_NumberOfOldPasswordToKeep_PrunesExistingHistory()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.User!.Settings.NumberOfOldPasswordToKeep = 0;

         IService service = database.User.AddService("RetentionService");
         IAccount account = service.AddAccount("Account", ["id@test"], "p0");
         Account concrete = (Account)account;
         concrete.Passwords[DateTime.Now.AddDays(-4)] = ProtectedSecret.Protect("p1");
         concrete.Passwords[DateTime.Now.AddDays(-3)] = ProtectedSecret.Protect("p2");
         concrete.Passwords[DateTime.Now.AddDays(-2)] = ProtectedSecret.Protect("p3");
         concrete.Passwords[DateTime.Now.AddDays(-1)] = ProtectedSecret.Protect("p4");

         _ = account.Passwords.Should().HaveCount(5);

         database.User.Settings.NumberOfOldPasswordToKeep = 2;

         _ = account.Passwords.Should().HaveCount(2);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * NumberOfMonthActivitiesToKeep drops old informational entries but keeps
       * activities that still need review.
      */
      public void Case03_NumberOfMonthActivitiesToKeep_PrunesOldActivities()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         Database core = (Database)database;
         string username = database.User!.Username;

         Activity oldInfo = new(DateTime.Now.AddYears(-2).Ticks, string.Empty, username, null, null, null, null, null, ActivityEventType.DatabaseSaved, needsReview: false);
         Activity oldReview = new(DateTime.Now.AddYears(-2).Ticks, string.Empty, username, null, null, "PasswordLevel", "1", null, ActivityEventType.LoginFailed, needsReview: true);
         core.ActivityCenter.Activities.Add(oldInfo);
         core.ActivityCenter.Activities.Add(oldReview);

         database.User.Settings.NumberOfMonthActivitiesToKeep = 1;

         IActivity[] remaining = core.ActivityCenter.GetActivitiesOrdered();
         _ = remaining.Should().NotContain(x => x.EventType == ActivityEventType.DatabaseSaved && x.DateTime.Year == oldInfo.DateTime.Year);
         _ = remaining.Should().Contain(x => x.NeedsReview && x.EventType == ActivityEventType.LoginFailed);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Language is stored on the user; empty means "follow application language".
       */
      public void Case04_Language_RoundTrip()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);

         _ = database.User!.Settings.Language.Should().BeEmpty();

         database.User.Settings.Language = "fr";
         database.Save();
         database.Close();

         IDatabase reopened = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = reopened.User!.Settings.Language.Should().Be("fr");

         reopened.User.Settings.Language = string.Empty;
         reopened.Save();
         reopened.Close();

         IDatabase again = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = again.User!.Settings.Language.Should().BeEmpty();

         again.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Theme is stored on the user; empty means "follow application theme".
      */
      public void Case05_Theme_RoundTrip()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);

         _ = database.User!.Settings.Theme.Should().BeEmpty();

         database.User.Settings.Theme = "Light";
         database.Save();
         database.Close();

         IDatabase reopened = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = reopened.User!.Settings.Theme.Should().Be("Light");

         reopened.User.Settings.Theme = string.Empty;
         reopened.Save();
         reopened.Close();

         IDatabase again = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = again.User!.Settings.Theme.Should().BeEmpty();

         again.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
