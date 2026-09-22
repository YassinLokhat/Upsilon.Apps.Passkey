using FluentAssertions;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   [TestClass]
   [DoNotParallelize]
   public sealed class IdentifierViewModelTests
   {
      private const string TestUsername = nameof(IdentifierViewModelTests);

      private IDatabase? _database;

      [TestInitialize]
      public void Initialize()
      {
         _ = Dispatcher.CurrentDispatcher;
         GuiTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(2);
         _database = UnitTestsHelper.CreateTestDatabase(passkeys, TestUsername);
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
      public void Case01_EditOneIdentifier_GraysOnlyThatRow()
      {
         IService service = _database!.User!.AddService("Svc");
         IAccount account = service.AddAccount(
            "Acc",
            [
               new Identifier(IdentifierType.Username, "alice"),
               new Identifier(IdentifierType.Email, "a@test.te"),
            ],
            "password");
         _database.Save();

         using AccountViewModel accountVm = new(account);
         IdentifierViewModel first = new(account, account.Identifiers.ElementAt(0));
         IdentifierViewModel second = new(account, account.Identifiers.ElementAt(1));
         accountVm.Identifiers.Add(first);
         accountVm.Identifiers.Add(second);
         accountVm.AddIdentifier(first);
         accountVm.AddIdentifier(second);

         _ = first.IdentifierBackground.Should().Be(FieldStateBrushes.UnchangedBrush2);
         _ = second.IdentifierBackground.Should().Be(FieldStateBrushes.UnchangedBrush2);

         first.Identifier = "bob";

         _ = account.HasChanged(nameof(account.Identifiers)).Should().BeTrue();
         _ = first.IdentifierBackground.Should().Be(FieldStateBrushes.ChangedBrush);
         _ = second.IdentifierBackground.Should().Be(FieldStateBrushes.UnchangedBrush2);
      }

      [TestMethod]
      public void Case02_AddIdentifier_GraysOnlyNewRow()
      {
         IService service = _database!.User!.AddService("Svc");
         IAccount account = service.AddAccount(
            "Acc",
            [new Identifier(IdentifierType.Username, "alice")],
            "password");
         _database.Save();

         using AccountViewModel accountVm = new(account);
         IdentifierViewModel existing = new(account, account.Identifiers.First());
         accountVm.Identifiers.Add(existing);
         accountVm.AddIdentifier(existing);

         accountVm.AddIdentifier(string.Empty);

         _ = accountVm.Identifiers.Should().HaveCount(2);
         _ = existing.IdentifierBackground.Should().Be(FieldStateBrushes.UnchangedBrush2);
         _ = accountVm.Identifiers[1].IdentifierBackground.Should().Be(FieldStateBrushes.ChangedBrush);
      }
   }
}
