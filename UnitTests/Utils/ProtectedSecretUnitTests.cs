using FluentAssertions;
using System.Text.Json;
using Upsilon.Apps.Passkey.Core.Utils;
using Upsilon.Apps.Passkey.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class ProtectedSecretUnitTests
   {
      [TestMethod]
      /*
       * Protect then Reveal round-trips the plaintext, including empty and null.
      */
      public void Case01_ProtectRevealRoundTrip()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            string source = UnitTestsHelper.GetRandomString();

            ProtectedSecret protectedSecret = ProtectedSecret.Protect(source);

            _ = protectedSecret.Reveal().Should().Be(source);
         }

         _ = ProtectedSecret.Protect(string.Empty).Reveal().Should().BeEmpty();
         _ = ProtectedSecret.Protect(null).Reveal().Should().BeEmpty();
      }

      [TestMethod]
      /*
       * Two Protect calls of the same plaintext remain independent wraps that
       * both reveal the same value.
      */
      public void Case02_ProtectProducesIndependentWraps()
      {
         string source = "same-secret";
         ProtectedSecret first = ProtectedSecret.Protect(source);
         ProtectedSecret second = ProtectedSecret.Protect(source);

         _ = first.Reveal().Should().Be(source);
         _ = second.Reveal().Should().Be(source);
         _ = ReferenceEquals(first, second).Should().BeFalse();
      }

      [TestMethod]
      /*
       * ToString must never expose the secret (logs, activity messages, debuggers).
      */
      public void Case03_ToStringIsRedacted()
      {
         string secret = UnitTestsHelper.GetRandomString();
         ProtectedSecret protectedSecret = ProtectedSecret.Protect(secret);

         _ = protectedSecret.ToString().Should().Be("***");
         _ = protectedSecret.ToString().Should().NotContain(secret);
         _ = $"{protectedSecret}".Should().Be("***");
      }

      [TestMethod]
      /*
       * The JSON converter writes plaintext (for the .pku onion) and re-protects on read.
      */
      public void Case04_JsonConverterRoundTrip()
      {
         string secret = UnitTestsHelper.GetRandomString();
         ProtectedSecret original = ProtectedSecret.Protect(secret);

         string json = UnitTestsHelper.SerializationCenter.Serialize(original);
         ProtectedSecret restored = UnitTestsHelper.SerializationCenter.Deserialize<ProtectedSecret>(json);

         _ = json.Should().Be(JsonSerializer.Serialize(secret));
         _ = restored.Reveal().Should().Be(secret);
         _ = restored.ToString().Should().Be("***");
      }

      [TestMethod]
      /*
       * A dictionary of ProtectedSecret values survives serialize/deserialize used
       * by account password history persistence.
      */
      public void Case05_DictionaryRoundTrip()
      {
         DateTime older = new(2020, 1, 15, 12, 0, 0, DateTimeKind.Utc);
         DateTime newer = new(2024, 6, 1, 8, 30, 0, DateTimeKind.Utc);
         Dictionary<DateTime, ProtectedSecret> history = new()
         {
            [older] = ProtectedSecret.Protect("old-password"),
            [newer] = ProtectedSecret.Protect("new-password"),
         };

         string json = UnitTestsHelper.SerializationCenter.Serialize(history);
         Dictionary<DateTime, ProtectedSecret> restored =
            UnitTestsHelper.SerializationCenter.Deserialize<Dictionary<DateTime, ProtectedSecret>>(json);

         _ = restored.Should().HaveCount(2);
         _ = restored[older].Reveal().Should().Be("old-password");
         _ = restored[newer].Reveal().Should().Be("new-password");
         _ = json.Should().Contain("old-password").And.Contain("new-password");
      }
   }
}
