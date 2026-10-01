using FluentAssertions;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Core
{
   [TestClass]
   public sealed class EditHistoryUnitTests
   {
      [TestMethod]
      /*
       * Field edits coalesce into one undo entry; Undo restores the original
       * value; Redo restores the edited value.
       */
      public void Case01_FieldEdit_UndoRedoRoundTrip()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         string original = service.ServiceName;

         service.ServiceName = "Renamed1";
         service.ServiceName = "Renamed2";

         _ = database.EditHistory.CanUndo.Should().BeTrue();
         _ = database.EditHistory.CanRedo.Should().BeFalse();

         database.EditHistory.Undo();

         _ = service.ServiceName.Should().Be(original);
         _ = database.EditHistory.CanUndo.Should().BeTrue(); // AddService still undoable
         _ = database.EditHistory.CanRedo.Should().BeTrue();

         database.EditHistory.Redo();

         _ = service.ServiceName.Should().Be("Renamed2");
         _ = database.EditHistory.CanRedo.Should().BeFalse();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Typing then reverting to the original value cancels the pending undo
       * entry for that field (same coalesce outcome as AutoSave).
       */
      public void Case02_FullRevert_DropsUndoEntryForField()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         string original = service.ServiceName;

         // Consume the AddService undo entry.
         database.EditHistory.Clear();

         service.ServiceName = "Temp";
         _ = database.EditHistory.CanUndo.Should().BeTrue();

         service.ServiceName = original;
         _ = database.EditHistory.CanUndo.Should().BeFalse();
         _ = service.HasChanged(nameof(service.ServiceName)).Should().BeFalse();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Undo of AddService removes the service; Redo brings it back.
       */
      public void Case03_AddService_UndoRedo()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         string itemId = service.ItemId;

         database.EditHistory.Undo();

         _ = database.User.Services.Should().BeEmpty();
         _ = database.EditHistory.CanRedo.Should().BeTrue();

         database.EditHistory.Redo();

         _ = database.User.Services.Should().ContainSingle(x => x.ItemId == itemId);
         _ = database.User.Services.Single().ServiceName.Should().Be(service.ServiceName);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * After Undo, a new user edit clears the redo stack.
       */
      public void Case04_NewEdit_ClearsRedo()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         database.EditHistory.Clear();

         service.Notes = "A";
         database.EditHistory.Undo();
         _ = database.EditHistory.CanRedo.Should().BeTrue();

         service.Notes = "B";
         _ = database.EditHistory.CanRedo.Should().BeFalse();
         _ = service.Notes.Should().Be("B");

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Close clears undo/redo. Explicit Save keeps the stack (session still open).
       */
      public void Case05_SaveKeepsStack_CloseClears()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         database.EditHistory.Clear();

         service.Notes = "kept-after-save";
         database.Save();

         _ = database.EditHistory.CanUndo.Should().BeTrue();

         database.EditHistory.Undo();
         _ = service.Notes.Should().BeEmpty();

         database.Close();

         // Re-open a fresh session; history must not survive Close.
         IDatabase reopened = UnitTestsHelper.OpenTestDatabase(passkeys, out _);
         _ = reopened.EditHistory.CanUndo.Should().BeFalse();
         _ = reopened.EditHistory.CanRedo.Should().BeFalse();

         reopened.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Username / Passkeys changes are not recorded on the undo stack.
       */
      public void Case06_Credentials_NotUndoable()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         database.EditHistory.Clear();

         string originalUsername = database.User!.Username;
         database.User.Username = originalUsername + "_renamed";

         _ = database.EditHistory.CanUndo.Should().BeFalse();

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Undo of a validated password commit restores the previous password.
       */
      public void Case07_PasswordCommit_Undo()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         string initialPassword = UnitTestsHelper.GetRandomString();
         IAccount account = service.AddAccount("Acc", UnitTestsHelper.Ids("id0"), initialPassword);
         database.EditHistory.Clear();

         string nextPassword = UnitTestsHelper.GetRandomString();
         account.Password = nextPassword;
         _ = account.Password.Should().Be(nextPassword);
         _ = database.EditHistory.CanUndo.Should().BeTrue();

         database.EditHistory.Undo();
         _ = account.Password.Should().Be(initialPassword);

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
