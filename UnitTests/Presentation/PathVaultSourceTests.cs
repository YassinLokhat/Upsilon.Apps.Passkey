using FluentAssertions;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace UnitTests.Presentation
{
   [TestClass]
   public sealed class PathVaultSourceTests
   {
      [TestMethod]
      public async Task PathVaultSource_WriteBack_IsSkipped()
      {
         string temp = Path.Combine(Path.GetTempPath(), $"pku-{Guid.NewGuid():N}.pku");
         try
         {
            await File.WriteAllTextAsync(temp, "x").ConfigureAwait(false);
            IVaultSource source = new PathVaultSource(temp);
            source.WorkingPath.Should().Be(Path.GetFullPath(temp));
            (await source.WriteBackAsync().ConfigureAwait(false)).Should().Be(VaultWriteBackResult.Skipped);
         }
         finally
         {
            if (File.Exists(temp))
            {
               File.Delete(temp);
            }
         }
      }
   }
}
