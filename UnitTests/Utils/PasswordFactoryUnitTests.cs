using FluentAssertions;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class PasswordFactoryUnitTests
   {
      [TestMethod]
      /*
       * The three built-in alphabets should be non-empty and mutually distinct
       * so that a caller can freely combine them.
      */
      public void Case01_BuiltInAlphabets()
      {
         // Given / When
         string alphabetic = UnitTestsHelper.PasswordFactory.Alphabetic;
         string numeric = UnitTestsHelper.PasswordFactory.Numeric;
         string specialChars = UnitTestsHelper.PasswordFactory.SpecialChars;

         // Then
         _ = alphabetic.Should().NotBeNullOrEmpty();
         _ = numeric.Should().Be("0123456789");
         _ = specialChars.Should().NotBeNullOrEmpty();

         _ = numeric.Any(alphabetic.Contains).Should().BeFalse();
      }

      [TestMethod]
      /*
       * A generated password should have the exact requested length.
      */
      public void Case02_GeneratePassword_RespectsLength()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            int length = UnitTestsHelper.GetRandomInt(1, 64);
            string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic
               + UnitTestsHelper.PasswordFactory.Numeric
               + UnitTestsHelper.PasswordFactory.SpecialChars;

            // When
            string password = UnitTestsHelper.PasswordFactory.GeneratePassword(length, alphabet, checkIfLeaked: false);

            // Then
            _ = password.Length.Should().Be(length);
         }
      }

      [TestMethod]
      /*
       * A generated password should only contain characters from the given alphabet.
      */
      public void Case03_GeneratePassword_OnlyUsesAlphabet()
      {
         for (int i = 0; i < UnitTestsHelper.RANDOMIZED_TESTS_LOOP; i++)
         {
            // Given
            string alphabet = UnitTestsHelper.PasswordFactory.Numeric;

            // When
            string password = UnitTestsHelper.PasswordFactory.GeneratePassword(32, alphabet, checkIfLeaked: false);

            // Then
            _ = password.ToCharArray().Should().OnlyContain(c => alphabet.Contains(c));
         }
      }

      [TestMethod]
      /*
       * Two consecutive generations of a long password should differ, proving the
       * generator draws fresh randomness each time.
      */
      public void Case04_GeneratePassword_IsRandom()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic
            + UnitTestsHelper.PasswordFactory.Numeric
            + UnitTestsHelper.PasswordFactory.SpecialChars;

         // When
         string first = UnitTestsHelper.PasswordFactory.GeneratePassword(64, alphabet, checkIfLeaked: false);
         string second = UnitTestsHelper.PasswordFactory.GeneratePassword(64, alphabet, checkIfLeaked: false);

         // Then
         _ = first.Should().NotBe(second);
      }

      [TestMethod]
      /*
       * A non-positive length yields an empty password.
      */
      public void Case05_GeneratePassword_NonPositiveLength()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Alphabetic;

         // When
         string zeroLength = UnitTestsHelper.PasswordFactory.GeneratePassword(0, alphabet, checkIfLeaked: false);
         string negativeLength = UnitTestsHelper.PasswordFactory.GeneratePassword(-5, alphabet, checkIfLeaked: false);

         // Then
         _ = zeroLength.Should().BeEmpty();
         _ = negativeLength.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * A blank alphabet yields an empty password.
      */
      public void Case06_GeneratePassword_BlankAlphabet()
      {
         // Given / When
         string emptyAlphabet = UnitTestsHelper.PasswordFactory.GeneratePassword(10, string.Empty, checkIfLeaked: false);
         string whitespaceAlphabet = UnitTestsHelper.PasswordFactory.GeneratePassword(10, "   ", checkIfLeaked: false);

         // Then
         _ = emptyAlphabet.Should().BeEmpty();
         _ = whitespaceAlphabet.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * The asynchronous generation honours the same contract as the synchronous
       * one: requested length, alphabet, and the empty-password guards.
      */
      public async Task Case07_GeneratePasswordAsync_MatchesSynchronousContract()
      {
         // Given
         string alphabet = UnitTestsHelper.PasswordFactory.Numeric;

         // When
         string password = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(32, alphabet, checkIfLeaked: false);
         string zeroLength = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(0, alphabet, checkIfLeaked: false);
         string blankAlphabet = await UnitTestsHelper.PasswordFactory.GeneratePasswordAsync(10, "   ", checkIfLeaked: false);

         // Then
         _ = password.Length.Should().Be(32);
         _ = password.ToCharArray().Should().OnlyContain(c => alphabet.Contains(c));
         _ = zeroLength.Should().BeEmpty();
         _ = blankAlphabet.Should().BeEmpty();
      }

      [TestMethod]
      /*
       * A cancelled leak check surfaces the cancellation instead of silently
       * reporting "not leaked", which is how a caller tells a deliberate abort
       * apart from an unreachable service.
      */
      public async Task Case08_PasswordLeakedAsync_HonoursCancellation()
      {
         // Given
         using CancellationTokenSource cancellation = new();
         await cancellation.CancelAsync();

         // When
         Func<Task> act = async () => await UnitTestsHelper.PasswordFactory
            .PasswordLeakedAsync(UnitTestsHelper.GetRandomString(), cancellation.Token);

         // Then
         _ = await act.Should().ThrowAsync<OperationCanceledException>();
      }
   }
}
