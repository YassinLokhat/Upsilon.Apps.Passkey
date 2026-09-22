using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Models;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Models
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

         Activity cleared = new(0xABCDEF012345, "Aitem", "an username", "a service name", "an account name", "a field name", "a field value", "a parent name", ActivityEventType.ItemUpdated, needsReview: false);
         Activity restoredCleared = new(cleared.ToString());
         _ = restoredCleared.NeedsReview.Should().BeFalse();
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
   }
}
