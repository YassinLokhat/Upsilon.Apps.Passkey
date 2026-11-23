using FluentAssertions;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// What the vault shows besides the Alerts menu: the title-bar seal, the
   /// per-service "needs attention" dots and the clipboard countdown strip.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class VaultStatusTests
   {
      private const string TestUsername = nameof(VaultStatusTests);

      private IDatabase? _database;

      [TestInitialize]
      public void Initialize()
      {
         _ = Dispatcher.CurrentDispatcher;
         GuiTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         _database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray(2), TestUsername);
         GuiTestServices.Session.StartSession(_database);
      }

      [TestCleanup]
      public void Cleanup()
      {
         GuiTestServices.Session.EndSession();
         _database = null;
         GuiTestServices.Reset();
         UnitTestsHelper.ClearTestEnvironment(TestUsername);
      }

      [TestMethod]
      public void Seal_ReflectsThePasskeyCount()
      {
         _ = UserServicesViewModel.SealFor(null).Should().Be(
            (Strings.Format(nameof(Strings.Msg_SealAtLeastPasskeys), AlertKinds.RecommendedPasskeyCount), false));
         _ = UserServicesViewModel.SealFor(new FakeInsufficient(0)).Should().Be((Strings.Msg_SealUsernameOnly, true));
         _ = UserServicesViewModel.SealFor(new FakeInsufficient(1)).Should().Be((Strings.Msg_SealOnePasskey, true));
         _ = UserServicesViewModel.SealFor(new FakeInsufficient(2)).Should().Be(
            (Strings.Format(nameof(Strings.Msg_SealPasskeys), 2), true));
      }

      [TestMethod]
      public void Attention_FlagsOnlyTheServicesOfAlertedAccounts()
      {
         IUser user = _database!.User!;
         IService alpha = user.AddService("Alpha");
         IService beta = user.AddService("Beta");
         IAccount alerted = alpha.AddAccount("A", UnitTestsHelper.Ids("a@test"), "pw-a-123456");
         _ = beta.AddAccount("B", UnitTestsHelper.Ids("b@test"), "pw-b-123456");

         using UserServicesViewModel vm = new("Test");
         vm.RefreshFilters();
         vm.ApplyAlerts([new FakeAccountsAlert(alerted)]);

         _ = vm.Services.Single(s => s.ServiceName == "Alpha").NeedsAttention.Should().BeTrue();
         _ = vm.Services.Single(s => s.ServiceName == "Beta").NeedsAttention.Should().BeFalse();
         _ = vm.ServiceNamesFor([new FakeAccountsAlert(alerted)]).Should().Be("Alpha");

         vm.ApplyAlerts([]);
         _ = vm.Services.Should().OnlyContain(s => !s.NeedsAttention);
      }

      [TestMethod]
      public void ClipboardStrip_CountsDownThenHides()
      {
         DateTime now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
         DateTime? pending = now.AddSeconds(8.5);
         ClipboardStatusViewModel vm = new(() => pending, () => now);
         int started = 0;
         vm.CountdownStarted += (_, _) => started++;

         vm.Refresh();
         _ = vm.IsActive.Should().BeTrue();
         _ = vm.Text.Should().Be(Strings.Format(nameof(Strings.Msg_ClipboardClears), 9));

         now = now.AddSeconds(3);
         vm.Refresh();
         _ = vm.Text.Should().Be(Strings.Format(nameof(Strings.Msg_ClipboardClears), 6));
         _ = started.Should().Be(1, "the same copy keeps its countdown");

         pending = null;
         vm.Refresh();
         _ = vm.IsActive.Should().BeFalse();
         vm.Stop();
      }

      private sealed class FakeInsufficient(int count) : IInsufficientPasskeysAlert
      {
         public int Count { get; } = count;
         public int RecommendedMinimum => AlertKinds.RecommendedPasskeyCount;
         public string Source => AlertKinds.SourceCore;
         public string Kind => AlertKinds.InsufficientPasskeys;
         public AlertSeverity Severity => AlertSeverity.Warning;
      }

      private sealed class FakeAccountsAlert(params IAccount[] accounts) : IAccountsAlert
      {
         public IEnumerable<IAccount> Accounts { get; } = accounts;
         public string Source => AlertKinds.SourceCore;
         public string Kind => AlertKinds.WeakAccountPassword;
         public AlertSeverity Severity => AlertSeverity.Warning;
      }
   }
}
