using FluentAssertions;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class ActivityCoalesceUnitTests
   {
      [TestMethod]
      /*
       * Successive ItemUpdated edits of the same field coalesce into one unsealed
       * activity (final readable value). A full revert drops that activity. A
       * different field keeps its own row. After Save seals the log, a new edit
       * of the same field appends a fresh activity rather than rewriting sealed
       * history. Password is not coalesced (validate-to-commit).
      */
      public void Case01_ItemUpdatedCoalescesPerField_AndSurvivesSealBoundary()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         IAccount account = service.AddAccount("Acc0", ["id0"], UnitTestsHelper.GetRandomString());
         string originalLabel = account.Label;

         int itemUpdatedBeforeEdits = database.Activities!
            .Count(x => x.EventType == ActivityEventType.ItemUpdated);

         account.Label = "L1";
         account.Label = "L12";
         account.Label = "L123";
         account.Notes = "N1";
         account.Notes = "N12";

         IActivity[] midTyping = [.. database.Activities!
            .Where(x => x.EventType == ActivityEventType.ItemUpdated)];

         _ = midTyping.Should().HaveCount(itemUpdatedBeforeEdits + 2);
         _ = midTyping.Should().ContainSingle(x => x.Message.Contains("label has been", StringComparison.OrdinalIgnoreCase)
            && x.Message.Contains("L123", StringComparison.Ordinal));
         _ = midTyping.Should().ContainSingle(x => x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase)
            && x.Message.Contains("N12", StringComparison.Ordinal));

         account.Label = originalLabel;

         IActivity[] afterRevert = [.. database.Activities!
            .Where(x => x.EventType == ActivityEventType.ItemUpdated)];

         _ = afterRevert.Should().HaveCount(itemUpdatedBeforeEdits + 1);
         _ = afterRevert.Should().NotContain(x => x.Message.Contains("label has been", StringComparison.OrdinalIgnoreCase));
         _ = afterRevert.Should().ContainSingle(x => x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase));
         _ = account.HasChanged(nameof(account.Label)).Should().BeFalse();
         _ = account.HasChanged(nameof(account.Notes)).Should().BeTrue();

         database.Save();

         int notesBeforePostSealEdit = database.Activities!
            .Count(x => x.EventType == ActivityEventType.ItemUpdated
               && x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase));
         int itemUpdatedAfterSave = database.Activities!
            .Count(x => x.EventType == ActivityEventType.ItemUpdated);

         account.Notes = "N123";

         IActivity[] afterSeal = [.. database.Activities!
            .Where(x => x.EventType == ActivityEventType.ItemUpdated)];

         _ = afterSeal.Should().HaveCount(itemUpdatedAfterSave + 1);
         _ = afterSeal.Count(x => x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase))
            .Should().Be(notesBeforePostSealEdit + 1);
         _ = afterSeal[0].Message.Should().Contain("N123");

         string password1 = UnitTestsHelper.GetRandomString();
         string password2 = UnitTestsHelper.GetRandomString();
         account.Password = password1;
         account.Password = password2;

         _ = database.Activities!
            .Count(x => x.EventType == ActivityEventType.ItemUpdated
               && x.Message.Contains("password has been", StringComparison.OrdinalIgnoreCase))
            .Should().BeGreaterThanOrEqualTo(2, "password activities must not coalesce");

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }

      [TestMethod]
      /*
       * A pause longer than the activity-log debounce must not seal partial
       * ItemUpdated rows: continuing to type the same field still coalesces into
       * one activity with the final value (the bug behind split "I" / full-text
       * notes entries).
      */
      public void Case02_ItemUpdatedStillCoalescesAfterDeferredDiskFlush()
      {
         UnitTestsHelper.ClearTestEnvironment();
         string[] passkeys = UnitTestsHelper.GetRandomStringArray();
         IDatabase database = UnitTestsHelper.CreateTestDatabase(passkeys);
         IService service = database.User!.AddService("Service_" + UnitTestsHelper.GetUsername());
         IAccount account = service.AddAccount("Acc0", ["id0"], UnitTestsHelper.GetRandomString());

         int notesBefore = database.Activities!
            .Count(x => x.EventType == ActivityEventType.ItemUpdated
               && x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase));

         account.Notes = "I";
         Thread.Sleep(700);
         account.Notes = "I'd like to tetst that";

         IActivity[] notesActivities = [.. database.Activities!
            .Where(x => x.EventType == ActivityEventType.ItemUpdated
               && x.Message.Contains("notes has been", StringComparison.OrdinalIgnoreCase))];

         _ = notesActivities.Should().HaveCount(notesBefore + 1);
         _ = notesActivities[0].Message.Should().Contain("I'd like to tetst that");
         _ = notesActivities.Should().NotContain(x => x.Message.EndsWith("set to I", StringComparison.Ordinal));

         database.Close();
         UnitTestsHelper.ClearTestEnvironment();
      }
   }
}
