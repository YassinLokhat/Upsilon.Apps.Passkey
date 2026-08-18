using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Models
{
   [TestClass]
   public sealed class ActivityUnitTests
   {
      [TestMethod]
      /*
       * ToString then the string constructor is a lossless round trip, including
       * pipe characters inside the payload (they must be escaped).
      */
      public void Case01_SerializationRoundTrip_EscapesPipes()
      {
         string[] data = ["item|name", "field", "value with | pipes", "parent"];
         Activity original = new(0xABCDEF012345, "Aitem", ActivityEventType.ItemUpdated, data, needsReview: true);

         Activity restored = new(original.ToString());

         _ = restored.DateTimeTicks.Should().Be(original.DateTimeTicks);
         _ = restored.ItemId.Should().Be(original.ItemId);
         _ = restored.EventType.Should().Be(original.EventType);
         _ = restored.NeedsReview.Should().BeTrue();
         _ = restored.Data.Should().Equal(data);
      }

      [TestMethod]
      /*
       * Every activity event type produces a non-empty human-readable message.
      */
      public void Case02_Message_CoversEveryEventType()
      {
         Dictionary<ActivityEventType, string[]> payloads = new()
         {
            [ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile] = ["alice"],
            [ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile] = ["alice"],
            [ActivityEventType.DontMergeAndRemoveAutoSaveFile] = ["alice"],
            [ActivityEventType.DontMergeAndKeepAutoSaveFile] = ["alice"],
            [ActivityEventType.DatabaseCreated] = ["alice"],
            [ActivityEventType.DatabaseOpened] = ["alice"],
            [ActivityEventType.DatabaseSaved] = ["alice"],
            [ActivityEventType.DatabaseClosed] = ["alice"],
            [ActivityEventType.LoginSessionTimeoutReached] = ["alice"],
            [ActivityEventType.LoginFailed] = ["alice", "2"],
            [ActivityEventType.UserLoggedIn] = ["alice"],
            [ActivityEventType.UserLoggedOut] = ["alice", ""],
            [ActivityEventType.ImportingDataStarted] = ["vault.json"],
            [ActivityEventType.ImportingDataSucceded] = [],
            [ActivityEventType.ImportingDataFailed] = ["bad format"],
            [ActivityEventType.ExportingDataStarted] = ["vault.csv"],
            [ActivityEventType.ExportingDataSucceded] = [],
            [ActivityEventType.ExportingDataFailed] = ["already exists"],
            [ActivityEventType.ItemUpdated] = ["Account", "Notes", "hello", "Service X"],
            [ActivityEventType.ItemAdded] = ["User alice", "", "Service X"],
            [ActivityEventType.ItemDeleted] = ["User alice", "", "Service X"],
            [ActivityEventType.ActivityLogTampered] = ["alice"],
            [ActivityEventType.None] = ["fallback"],
         };

         foreach (ActivityEventType eventType in Enum.GetValues<ActivityEventType>())
         {
            string[] data = payloads[eventType];
            Activity activity = new(DateTime.Now.Ticks, "id", eventType, data, needsReview: false);

            _ = activity.Message.Should().NotBeNullOrWhiteSpace($"event {eventType} must render a message");
         }

         Activity loggedOutDirty = new(DateTime.Now.Ticks, "id", ActivityEventType.UserLoggedOut, ["alice", "1"], needsReview: true);
         _ = loggedOutDirty.Message.Should().Contain("without saving");

         Activity updatedBlank = new(DateTime.Now.Ticks, "id", ActivityEventType.ItemUpdated, ["Account", "Notes", ""], needsReview: false);
         _ = updatedBlank.Message.Should().Contain("updated");
         _ = updatedBlank.Message.Should().NotContain("set to");
      }

      [TestMethod]
      /*
       * A truncated serialized form still constructs without throwing.
      */
      public void Case03_Constructor_PartialPayload()
      {
         Activity activity = new("FF");

         _ = activity.DateTimeTicks.Should().Be(0xFF);
         _ = activity.ItemId.Should().BeEmpty();
         _ = activity.EventType.Should().Be(ActivityEventType.None);
         _ = activity.Data.Should().BeEmpty();
      }
   }
}
