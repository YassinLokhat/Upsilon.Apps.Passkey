using FluentAssertions;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class SecretQualityUnitTests
   {
      [TestMethod]
      public void Evaluate_StrongSecret_ReturnsNone()
      {
         SecretQualityIssue issues = SecretQuality.Evaluate("Correct-Horse-Battery1");
         _ = issues.Should().Be(SecretQualityIssue.None);
         _ = SecretQuality.IsWeak("Correct-Horse-Battery1").Should().BeFalse();
      }

      [TestMethod]
      public void Evaluate_TooShort_FlagsTooShort()
      {
         SecretQualityIssue issues = SecretQuality.Evaluate("Ab1!");
         _ = issues.Should().HaveFlag(SecretQualityIssue.TooShort);
         _ = SecretQuality.IsWeak("Ab1!").Should().BeTrue();
      }

      [TestMethod]
      public void Evaluate_LowDiversity_FlagsLowDiversity()
      {
         // 12+ chars but only lowercase letters → one character class.
         SecretQualityIssue issues = SecretQuality.Evaluate("abcdefghijkl");
         _ = issues.Should().HaveFlag(SecretQualityIssue.LowDiversity);
         _ = issues.Should().NotHaveFlag(SecretQualityIssue.TooShort);
      }

      [TestMethod]
      public void Evaluate_MatchesUsername_FlagsMatchesUsername()
      {
         SecretQualityIssue issues = SecretQuality.Evaluate("MyUserName12!", "myusername12!");
         _ = issues.Should().HaveFlag(SecretQualityIssue.MatchesUsername);
      }

      [TestMethod]
      public void Evaluate_TrivialAscending_FlagsTrivialPattern()
      {
         SecretQualityIssue issues = SecretQuality.Evaluate("abcdefghijkl");
         _ = issues.Should().HaveFlag(SecretQualityIssue.TrivialPattern);
      }

      [TestMethod]
      public void Evaluate_Empty_FlagsTooShortAndLowDiversity()
      {
         SecretQualityIssue issues = SecretQuality.Evaluate(string.Empty);
         _ = issues.Should().HaveFlag(SecretQualityIssue.TooShort);
         _ = issues.Should().HaveFlag(SecretQualityIssue.LowDiversity);
      }
   }
}
