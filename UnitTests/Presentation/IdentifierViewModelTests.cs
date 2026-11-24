using FluentAssertions;
using UnitTests.Presentation.Fakes;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;
using Upsilon.Apps.Passkey.GUI.Presentation.ViewModels.Vault;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;
using Upsilon.Apps.Passkey.UnitTests.Multiplateform;

namespace UnitTests.Presentation
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
         ViewModelTestServices.Install();

         UnitTestsHelper.ClearTestEnvironment(TestUsername);
         string[] passkeys = UnitTestsHelper.GetRandomStringArray(2);
         _database = UnitTestsHelper.CreateTestDatabase(passkeys, TestUsername);
         AppServices.Session.StartSession(_database);
      }

      [TestCleanup]
      public void Cleanup()
      {
         _database = null;
         ViewModelTestServices.Reset();
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

         _ = first.IdentifierState.Should().Be(FieldVisualState.Default);
         _ = second.IdentifierState.Should().Be(FieldVisualState.Default);

         first.Identifier = "bob";

         _ = account.HasChanged(nameof(account.Identifiers)).Should().BeTrue();
         _ = first.IdentifierState.Should().Be(FieldVisualState.Dirty);
         _ = second.IdentifierState.Should().Be(FieldVisualState.Default);
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
         _ = existing.IdentifierState.Should().Be(FieldVisualState.Default);
         _ = accountVm.Identifiers[1].IdentifierState.Should().Be(FieldVisualState.Dirty);
      }
   }
}
