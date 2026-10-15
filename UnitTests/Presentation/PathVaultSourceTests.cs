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

      [TestMethod]
      public async Task Delete_RemovesOnlyThisVaultsReplaceLeftovers()
      {
         string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"pku-{Guid.NewGuid():N}")).FullName;
         try
         {
            string vault = Path.Join(dir, "a.pku");
            string ownTmp = $"{vault}.{Guid.NewGuid():N}.tmp";
            string[] untouched =
            [
               Path.Join(dir, $"a.pku2.{Guid.NewGuid():N}.tmp"),
               Path.Join(dir, $"b.pku.{Guid.NewGuid():N}.tmp"),
               $"{vault}.notahexguid.tmp",
               Path.Join(dir, "b.pku"),
            ];
            foreach (string file in untouched.Append(ownTmp))
            {
               await File.WriteAllTextAsync(file, "x");
            }

            PathVaultSource source = new(vault);
            _ = (await source.DeleteAsync()).Should().BeTrue();

            _ = File.Exists(ownTmp).Should().BeFalse();
            _ = untouched.Should().OnlyContain(x => File.Exists(x));

            await File.WriteAllTextAsync(vault, "x");
            _ = (await source.DeleteAsync()).Should().BeFalse("Core did not delete the vault");
         }
         finally
         {
            Directory.Delete(dir, recursive: true);
         }
      }

      [TestMethod]
      public async Task Rename_NeverOverwritesAndRejectsPaths()
      {
         string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"pku-{Guid.NewGuid():N}")).FullName;
         try
         {
            string vault = Path.Join(dir, "old.pku");
            await File.WriteAllTextAsync(vault, "vault");
            await File.WriteAllTextAsync(Path.Join(dir, "taken.pku"), "other");
            PathVaultSource source = new(vault);

            _ = (await source.RenameAsync("taken.pku")).Should().BeFalse();
            _ = (await source.RenameAsync(Path.Join("..", "escape.pku"))).Should().BeFalse();
            _ = (await File.ReadAllTextAsync(Path.Join(dir, "taken.pku"))).Should().Be("other");

            _ = (await source.RenameAsync("new.pku")).Should().BeTrue();
            _ = source.WorkingPath.Should().Be(Path.Join(dir, "new.pku"));
            _ = source.Location.Should().Be(source.WorkingPath);
            _ = File.Exists(vault).Should().BeFalse();
         }
         finally
         {
            Directory.Delete(dir, recursive: true);
         }
      }
   }
}
