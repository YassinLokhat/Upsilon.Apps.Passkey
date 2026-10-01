using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   [TestClass]
   public sealed class CredentialsConfirmationViewModelTests
   {
      [TestMethod]
      public void ValidateCredentials_AcceptsFullSequenceInOrder()
      {
         CredentialsConfirmationViewModel vm = new(["alice", "one", "two"], isNew: true);

         vm.ValidateCredentials("alice").Should().BeFalse();
         vm.IsAwaitingPasskeys.Should().BeTrue();
         vm.ValidateCredentials("one").Should().BeFalse();
         vm.ValidateCredentials("two").Should().BeTrue();
      }

      [TestMethod]
      public void ValidateCredentials_MistypePoisonsSequenceUntilClear_LikeLogin()
      {
         CredentialsConfirmationViewModel vm = new(["alice", "one"], isNew: false);

         vm.ValidateCredentials("bob").Should().BeFalse();
         vm.IsAwaitingPasskeys.Should().BeTrue();

         // Later correct factors cannot recover a poisoned sequence (same idea as progressive login).
         vm.ValidateCredentials("one").Should().BeFalse();

         vm.ClearCredentials();
         vm.IsAwaitingPasskeys.Should().BeFalse();
         vm.ValidateCredentials("alice").Should().BeFalse();
         vm.ValidateCredentials("one").Should().BeTrue();
      }

      [TestMethod]
      public void ClearCredentials_ResetsToUsernameStep()
      {
         CredentialsConfirmationViewModel vm = new(["alice", "one"], isNew: true);

         _ = vm.ValidateCredentials("alice");
         vm.IsAwaitingPasskeys.Should().BeTrue();

         vm.ClearCredentials();
         vm.IsAwaitingPasskeys.Should().BeFalse();
         vm.ValidateCredentials("alice").Should().BeFalse();
         vm.ValidateCredentials("one").Should().BeTrue();
      }
   }
}
