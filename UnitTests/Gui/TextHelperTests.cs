using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Gui
{
   [TestClass]
   public sealed class TextHelperTests
   {
      [TestMethod]
      public void ToSentenceCase_UppercasesFirstCharacter()
      {
         _ = TextHelper.ToSentenceCase("service name").Should().Be("Service name");
         _ = TextHelper.ToSentenceCase("Password update reminder delay").Should().Be("Password update reminder delay");
         _ = TextHelper.ToSentenceCase("notes").Should().Be("Notes");
         _ = TextHelper.ToSentenceCase(string.Empty).Should().BeEmpty();
      }
   }
}
