using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Core
{
   [TestClass]
   public sealed class ResourceBudgetsUnitTests
   {
      [TestMethod]
      public void CopyBounded_UnderLimit_Succeeds()
      {
         byte[] data = new byte[1000];
         using MemoryStream input = new(data);
         using MemoryStream output = new();
         ResourceBudgets.CopyBounded(input, output, limit: 1000);
         output.Length.Should().Be(1000);
      }

      [TestMethod]
      public void CopyBounded_OverLimit_ThrowsInvalidData()
      {
         byte[] data = new byte[100];
         using MemoryStream input = new(data);
         using MemoryStream output = new();
         Action act = () => ResourceBudgets.CopyBounded(input, output, limit: 50);
         act.Should().Throw<InvalidDataException>();
      }
   }
}
