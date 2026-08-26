using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class StaticMethodsUnitTests
   {
      [TestMethod]
      /*
       * SerializeWith then DeserializeTo should be a lossless round trip.
      */
      public void Case01_SerializeThenDeserialize()
      {
         // Given
         List<int> source = [1, 2, 3, 42];

         // When
         string serialized = source.SerializeWith(UnitTestsHelper.SerializationCenter);
         List<int> deserialized = serialized.DeserializeTo<List<int>>(UnitTestsHelper.SerializationCenter);

         // Then
         _ = deserialized.Should().Equal(source);
      }

      [TestMethod]
      /*
       * CloneWith should return a deep copy: equal by value but a different reference.
      */
      public void Case02_CloneWith_DeepCopy()
      {
         // Given
         List<string> source = ["a", "b", "c"];

         // When
         List<string> clone = source.CloneWith(UnitTestsHelper.SerializationCenter);

         // Then
         _ = clone.Should().Equal(source);
         _ = clone.Should().NotBeSameAs(source);

         // Mutating the clone must not affect the source.
         clone.Add("d");
         _ = source.Should().HaveCount(3);
      }

      [TestMethod]
      /*
       * AreDifferent should compare by serialized content, not by reference.
      */
      public void Case03_AreDifferent()
      {
         // Given
         List<int> reference = [1, 2, 3];
         List<int> equalByValue = [1, 2, 3];
         List<int> different = [1, 2, 4];

         // When / Then
         _ = UnitTestsHelper.SerializationCenter.AreDifferent(reference, equalByValue).Should().BeFalse();
         _ = UnitTestsHelper.SerializationCenter.AreDifferent(reference, different).Should().BeTrue();
      }
   }
}
