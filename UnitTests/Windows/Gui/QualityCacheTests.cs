using FluentAssertions;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// The quality ticks are worked out once per password: editing the label, the
   /// notes or an identifier, or an alert update, must not read (reveal) the
   /// password again.
   /// </summary>
   [TestClass]
   [DoNotParallelize]
   public sealed class QualityCacheTests
   {
      private const string TestUsername = nameof(QualityCacheTests);

      private IDatabase? _database;
      private int _evaluations;
      private Func<string, string?, SecretQualityIssue> _original = null!;

      [TestInitialize]
      public void Initialize()
      {
         _ = Dispatcher.CurrentDispatcher;
         GuiTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         _database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray(2), TestUsername);
         GuiTestServices.Session.StartSession(_database);

         _original = AccountViewModel.EvaluateQuality;
         _evaluations = 0;
         AccountViewModel.EvaluateQuality = (secret, username) =>
         {
            _evaluations++;
            return _original(secret, username);
         };
      }

      [TestCleanup]
      public void Cleanup()
      {
         AccountViewModel.EvaluateQuality = _original;
         GuiTestServices.Session.EndSession();
         _database = null;
         GuiTestServices.Reset();
         UnitTestsHelper.ClearTestEnvironment(TestUsername);
      }

      [TestMethod]
      public void EditingNotesOrLabel_DoesNotReEvaluateThePassword()
      {
         IService service = _database!.User!.AddService("Svc");
         IAccount account = service.AddAccount("Acc", UnitTestsHelper.Ids("a@test"), "Correct-Horse-42");

         using AccountViewModel vm = new(account);

         _ = vm.QualityChecks.Should().NotBeEmpty();
         _ = _evaluations.Should().Be(1, "worked out once when the account is shown");

         vm.Notes = "recovery codes in the safe";
         vm.Label = "Work";
         _ = vm.QualityChecks;
         _ = vm.QualityChecks;

         _ = _evaluations.Should().Be(1, "notes, label and repeated reads reuse the cached result");
      }

      [TestMethod]
      public void ChangingThePassword_EvaluatesTheNewOneOnce()
      {
         IService service = _database!.User!.AddService("Svc");
         IAccount account = service.AddAccount("Acc", UnitTestsHelper.Ids("a@test"), "short");

         using AccountViewModel vm = new(account);
         _ = vm.QualityChecks;

         vm.Password = "Much-Longer-Password-2026";
         IReadOnlyList<QualityCheck> checks = vm.QualityChecks;

         _ = _evaluations.Should().Be(2);
         _ = checks[0].State.Should().Be(QualityState.Pass, "the new password is long enough");
      }
   }
}
