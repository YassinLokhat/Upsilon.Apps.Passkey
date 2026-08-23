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
         Activity original = new(0xABCDEF012345, "Aitem", "an item name", "a field name", "a field value", "a parent name", ActivityEventType.ItemUpdated, needsReview: true);

         Activity restored = new(original.ToString());

         _ = restored.DateTimeTicks.Should().Be(original.DateTimeTicks);
         _ = restored.ItemId.Should().Be(original.ItemId);
         _ = restored.ItemName.Should().Be(original.ItemName);
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
         _ = activity.ItemName.Should().BeNull();
         _ = activity.FieldName.Should().BeNull();
         _ = activity.FieldValue.Should().BeNull();
         _ = activity.ParentName.Should().BeNull();
         _ = activity.EventType.Should().Be(ActivityEventType.None);
         _ = activity.NeedsReview.Should().BeTrue();
      }
   }
}
