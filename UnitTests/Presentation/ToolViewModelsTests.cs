using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Tools;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace UnitTests.Presentation
{
   [TestClass]
   [DoNotParallelize]
   public sealed class ToolViewModelsTests
   {
      [TestCleanup]
      public void Cleanup() => AppServices.Reset();

      [TestMethod]
      public void QrCode_ExpiresOnce_ClearsMatrix_AndIgnoresLateTicks()
      {
         FakeTimerFactory timers = new();
         QrCodeViewModel vm = new("secret", 1500, timers);
         int expired = 0;
         vm.Expired += (s, e) => expired++;

         vm.Modules.Should().NotBeEmpty();
         timers.Last!.Interval.Should().Be(TimeSpan.FromMilliseconds(1500));
         timers.Last.IsRunning.Should().BeTrue();

         timers.Last.Fire();
         timers.Last.Fire();

         expired.Should().Be(1);
         vm.Modules.Should().BeEmpty();
         vm.IsClosed.Should().BeTrue();
         timers.Last.IsRunning.Should().BeFalse();
         timers.Last.IsDisposed.Should().BeTrue();
      }

      [TestMethod]
      public void QrCode_EarlyClose_StopsTimer_AndNeverRaisesExpired()
      {
         FakeTimerFactory timers = new();
         QrCodeViewModel vm = new("secret", 1000, timers);
         bool expired = false;
         vm.Expired += (s, e) => expired = true;

         vm.Close();
         timers.Last!.Fire();

         expired.Should().BeFalse();
         timers.Last.IsRunning.Should().BeFalse();
         vm.Modules.Should().BeEmpty();
      }

      [TestMethod]
      [DataRow(0)]
      [DataRow(-5)]
      public void QrCode_NonPositiveDelay_CreatesNoTimer(int delay)
      {
         FakeTimerFactory timers = new();
         QrCodeViewModel vm = new("secret", delay, timers);

         timers.Last.Should().BeNull();
         vm.Modules.Should().NotBeEmpty();
      }

      [TestMethod]
      public void InsertIdentifier_OrdersPrefixFirst_FiltersByType_AndTrims()
      {
         IIdentifier[] known =
         [
            new Identifier(IdentifierType.Email, "bob@alice.io"),
            new Identifier(IdentifierType.Username, "alice"),
            new Identifier(IdentifierType.Email, "alice@x.io"),
         ];
         InsertIdentifierViewModel vm = new(known, null)
         {
            Type = InsertIdentifierViewModel.AllIdentifierType,
            Identifier = "ALI",
         };

         vm.Identifiers.Should().Equal("alice", "alice@x.io", "bob@alice.io");

         vm.Type = IdentifierType.Email;
         vm.Identifiers.Should().Equal("alice@x.io", "bob@alice.io");

         vm.Identifier = "  new  ";
         vm.ToIdentifier().Value.Should().Be("new");
         vm.ToIdentifier().Type.Should().Be(IdentifierType.Email);
      }

      [TestMethod]
      public void PasswordGenerator_Close_ClearsSecret_AndDropsLateResults()
      {
         PasswordGeneratorViewModel vm = new() { CheckIfLeaked = false };
         vm.GeneratedPassword = "typed-secret";

         vm.Close();
         vm.GeneratedPassword.Should().BeEmpty();

         vm.PasswordLength = 30;
         vm.GeneratedPassword.Should().BeEmpty();
      }
   }
}
