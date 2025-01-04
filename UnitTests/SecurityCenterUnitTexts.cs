using FluentAssertions;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.PassKey.UnitTests
{
   [TestClass]
   public sealed class SecurityCenterUnitTexts
   {
      [TestMethod]
      /*
       * Signing an empty string returns the revert of hash code of that empty string,
       * Then checking the signature returns the empty string.
      */
      public void Case01_SignEmptyString()
      {
         // Given
         string source = string.Empty;

         // When
         string signedSource = source.Sign();

         // Then
         signedSource.Should().Be(new string(string.Empty.GetHash().Reverse().ToArray()));

         // When
         string checkedSource = signedSource.CheckSign();

         // Then
         checkedSource.Should().Be(source);
      }
   }
}
