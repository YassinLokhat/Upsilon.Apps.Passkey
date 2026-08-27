using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.GUI.WPF.ViewModels.Controls;
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
         Activity original = new(0xABCDEF012345, "Aitem", "an username", "a service name", "an account name", "a field name", "a field value", "a parent name", ActivityEventType.ItemUpdated, needsReview: true);

         Activity restored = new(original.ToString());

         _ = restored.DateTimeTicks.Should().Be(original.DateTimeTicks);
         _ = restored.ItemId.Should().Be(original.ItemId);
         _ = restored.Username.Should().Be(original.Username);
         _ = restored.ServiceName.Should().Be(original.ServiceName);
         _ = restored.AccountName.Should().Be(original.AccountName);
         _ = restored.FieldName.Should().Be(original.FieldName);
         _ = restored.FieldValue.Should().Be(original.FieldValue);
         _ = restored.ParentName.Should().Be(original.ParentName);
         _ = restored.EventType.Should().Be(original.EventType);
         _ = restored.NeedsReview.Should().BeTrue();
      }

      [TestMethod]
      /*
       * A truncated serialized form still constructs without throwing.
      */
      public void Case02_Constructor_PartialPayload()
      {
         Activity activity = new("FF");

         _ = activity.DateTimeTicks.Should().Be(0xFF);
         _ = activity.ItemId.Should().BeEmpty();
         _ = activity.Username.Should().BeNull();
         _ = activity.ServiceName.Should().BeNull();
         _ = activity.AccountName.Should().BeNull();
         _ = activity.FieldName.Should().BeNull();
         _ = activity.FieldValue.Should().BeNull();
         _ = activity.ParentName.Should().BeNull();
         _ = activity.EventType.Should().Be(ActivityEventType.None);
         _ = activity.NeedsReview.Should().BeTrue();
      }

      [TestMethod]
      /*
       * Every activity event type produces a non-empty human-readable message.
      */
      public void Case03_Message_CoversEveryEventType()
      {
         Dictionary<ActivityEventType, string[]> payloads = new()
         {
            [ActivityEventType.MergeAndSaveThenRemoveAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.MergeWithoutSavingAndKeepAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DontMergeAndRemoveAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DontMergeAndKeepAutoSaveFile] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseCreated] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseOpened] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseSaved] = ["alice", null, null, null, null, null],
            [ActivityEventType.DatabaseClosed] = ["alice", null, null, null, null, null],
            [ActivityEventType.LoginSessionTimeoutReached] = ["alice", null, null, null, null, null],
            [ActivityEventType.LoginFailed] = ["alice", null, null, null, "2", null],
            [ActivityEventType.UserLoggedIn] = ["alice", null, null, null, null, null],
            [ActivityEventType.UserLoggedOut] = ["alice", null, null, null, null, null],
            [ActivityEventType.ImportingDataStarted] = [null, null, null, null, "vault.json", null],
            [ActivityEventType.ImportingDataSucceded] = [null, null, null, null, null, null],
            [ActivityEventType.ImportingDataFailed] = [null, null, null, null, "bad format", null],
            [ActivityEventType.ExportingDataStarted] = [null, null, null, null, "vault.csv", null],
            [ActivityEventType.ExportingDataSucceded] = [null, null, null, null, null, null],
            [ActivityEventType.ExportingDataFailed] = [null, null, null, null, "already exists", null],
            [ActivityEventType.ItemUpdated] = [null, null, "Account", "Notes", "hello", "Service X"],
            [ActivityEventType.ItemAdded] = ["User alice", null, null, null, "Service X", null],
            [ActivityEventType.ItemDeleted] = ["User alice", null, null, null, "Service X", null],
            [ActivityEventType.ActivityLogTampered] = ["alice", null, null, null, null, null],
            [ActivityEventType.None] = [null, null, null, null, "fallback", null],
         };

         foreach (ActivityEventType eventType in Enum.GetValues<ActivityEventType>())
         {
            string[] data = payloads[eventType];
            ActivityViewModel activity = new(new Activity(DateTime.Now.Ticks, "id", data[0], data[1], data[2], data[3], data[4], data[4], eventType, needsReview: false));

            _ = activity.Message.Should().NotBeNullOrWhiteSpace($"event {eventType} must render a message");
         }

         ActivityViewModel loggedOutDirty = new(new Activity(DateTime.Now.Ticks, "id", "alice", null, null, "needsReview", "1", null, ActivityEventType.UserLoggedOut, needsReview: true));
         _ = loggedOutDirty.Message.Should().Contain("without saving");

         ActivityViewModel updatedBlank = new(new Activity(DateTime.Now.Ticks, "id", null, null, "Account", "Notes", null, null, ActivityEventType.ItemUpdated, needsReview: false));
         _ = updatedBlank.Message.Should().Contain("updated");
         _ = updatedBlank.Message.Should().NotContain("set to");
      }
   }
}
