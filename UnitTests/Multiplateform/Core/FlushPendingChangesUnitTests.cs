using FluentAssertions;
using System.IO.Compression;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Core
{
   [TestClass]
   public sealed class FlushPendingChangesUnitTests
   {
      [TestMethod]
      /*
       * FlushPendingChanges writes the debounced autosave entry immediately, and
       * a host can copy a complete vault while the session keeps it open.
      */
      public void Case01_FlushPendingChanges_WritesAutoSaveReadableWhileOpen()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         string snapshot = Path.GetTempFileName();

         try
         {
            _ = database.User!.AddService(UnitTestsHelper.GetRandomString());
            database.FlushPendingChanges();

            using (FileStream source = new(UnitTestsHelper.ComputeDatabaseFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (FileStream target = File.Create(snapshot))
            {
               source.CopyTo(target);
            }

            using ZipArchive archive = ZipFile.OpenRead(snapshot);
            _ = archive.GetEntry("autosave").Should().NotBeNull();
         }
         finally
         {
            database.Close();
            File.Delete(snapshot);
            UnitTestsHelper.ClearTestEnvironment();
         }
      }

      [TestMethod]
      /*
       * Without a logged-in user, FlushPendingChanges is a no-op.
      */
      public void Case02_FlushPendingChanges_AfterCloseDoesNotThrow()
      {
         UnitTestsHelper.ClearTestEnvironment();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
         database.Close();

         database.Invoking(x => x.FlushPendingChanges()).Should().NotThrow();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * Closing twice (or closing after Delete) is a no-op: DatabaseClosed is
       * raised once and nothing is written to the released file.
      */
      public void Case03_Close_IsIdempotentAfterCloseAndDelete()
      {
         UnitTestsHelper.ClearTestEnvironment();
         IDatabase closed = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
         int closedEvents = 0;
         closed.DatabaseClosed += (_, _) => closedEvents++;

         closed.Close();
         closed.Invoking(x => x.Close()).Should().NotThrow();
         _ = closedEvents.Should().Be(1);
         UnitTestsHelper.ClearTestEnvironment();

         IDatabase deleted = UnitTestsHelper.CreateTestDatabase(UnitTestsHelper.GetRandomStringArray());
         deleted.Delete();
         deleted.Invoking(x => x.Close()).Should().NotThrow();
         _ = File.Exists(UnitTestsHelper.ComputeDatabaseFilePath()).Should().BeFalse();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
