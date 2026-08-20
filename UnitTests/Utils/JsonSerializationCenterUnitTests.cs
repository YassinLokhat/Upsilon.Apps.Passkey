using FluentAssertions;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class JsonSerializationCenterUnitTests
   {
      [TestMethod]
      /*
       * Serializing then deserializing a value should return an equivalent value.
      */
      public void Case01_RoundTrip_PreservesValues()
      {
         // Given
         Dictionary<string, int> source = new()
         {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
         };

         // When
         string serialized = UnitTestsHelper.SerializationCenter.Serialize(source);
         Dictionary<string, int> deserialized = UnitTestsHelper.SerializationCenter.Deserialize<Dictionary<string, int>>(serialized);

         // Then
         _ = deserialized.Should().BeEquivalentTo(source);
      }

      [TestMethod]
      /*
       * Enums should be serialized by their name (JsonStringEnumConverter) and not
       * by their numeric value, and round trip back to the same enum value.
      */
      public void Case02_Enum_SerializedAsName()
      {
         // Given
         AccountOption option = AccountOption.WarnIfPasswordLeaked;

         // When
         string serialized = UnitTestsHelper.SerializationCenter.Serialize(option);
         AccountOption deserialized = UnitTestsHelper.SerializationCenter.Deserialize<AccountOption>(serialized);

         // Then
         _ = serialized.Should().Contain(nameof(AccountOption.WarnIfPasswordLeaked));
         _ = serialized.Should().NotContain("1");
         _ = deserialized.Should().Be(option);
      }

      [TestMethod]
      /*
       * Deserializing a JSON "null" literal to a reference type should surface a
       * NullValueException rather than returning null.
      */
      public void Case03_Deserialize_NullThrows()
      {
         // Given / When
         Action act = new(() => UnitTestsHelper.SerializationCenter.Deserialize<List<string>>("null"));

         // Then
         _ = act.Should().Throw<NullValueException>();
      }

      [TestMethod]
      /*
       * Deserializing malformed JSON should throw.
      */
      public void Case04_Deserialize_MalformedThrows()
      {
         // Given / When
         Action act = new(() => UnitTestsHelper.SerializationCenter.Deserialize<Dictionary<string, int>>("{ not json"));

         // Then
         _ = act.Should().Throw<Exception>();
      }

      [TestMethod]
      /*
       * A list of random strings should survive a serialization round trip.
      */
      public void Case05_RoundTrip_RandomStrings()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string[] source = UnitTestsHelper.GetRandomStringArray();

            // When
            string serialized = UnitTestsHelper.SerializationCenter.Serialize(source);
            string[] deserialized = UnitTestsHelper.SerializationCenter.Deserialize<string[]>(serialized);

            // Then
            _ = deserialized.Should().Equal(source);
         }
      }
   }
}
