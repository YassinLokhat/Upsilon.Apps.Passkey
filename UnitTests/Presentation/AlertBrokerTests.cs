using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Alerts;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class AlertBrokerTests
   {
      [TestMethod]
      /*
       * CoreAlertsScanCompleted must raise NotifiedAlertsChanged so the menu
       * path cannot stay stale after kinds were stored.
      */
      public void ScanCompleted_RaisesNotifiedAlertsChanged_WithFilteredKinds()
      {
         ViewModelTestServices.Install();
         try
         {
            UnitTestsHelper.ClearTestEnvironment();
            string[] passkeys = UnitTestsHelper.GetRandomStringArray();
            IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
            database.User!.Settings.AlertsToNotify = new AlertKindList([AlertKinds.DuplicatedPasswords]);

            IService service = database.User.AddService("BrokerService");
            IAccount a = service.AddAccount("A", UnitTestsHelper.Ids("a@test"), "shared-secret");
            IAccount b = service.AddAccount("B", UnitTestsHelper.Ids("b@test"), "shared-secret");
            a.Options = AccountOption.WarnIfDuplicatedPassword;
            b.Options = AccountOption.WarnIfDuplicatedPassword;

            ISessionService session = AppServices.Session;
            session.StartSession(database);

            AlertBroker broker = session.Alerts;
            int notifications = 0;
            broker.NotifiedAlertsChanged += (_, _) => Interlocked.Increment(ref notifications);

            _ = UnitTestsHelper.WaitForAlertKind(
               database,
               AlertKinds.DuplicatedPasswords,
               database.RefreshAlerts);

            _ = notifications.Should().BeGreaterThan(0);
            _ = broker.GetNotifiedAlerts(AlertKinds.DuplicatedPasswords)
               .OfType<IAccountsAlert>()
               .Single()
               .Accounts.Should().BeEquivalentTo([a, b]);

            session.EndSession();
            UnitTestsHelper.ClearTestEnvironment();
         }
         finally
         {
            ViewModelTestServices.Reset();
         }
      }
   }
}
