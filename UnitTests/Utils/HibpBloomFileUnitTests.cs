using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class HibpBloomFileUnitTests
   {
      [TestMethod]
      public void Case01_BloomSizing_OnePercentUsesSevenHashes()
      {
         (ulong bits, int k) = BloomSizing.For(1_000_000, 0.01);
         _ = k.Should().Be(7);
         _ = bits.Should().BeGreaterThan(9_000_000UL);
      }

      [TestMethod]
      public void Case02_NoFalseNegatives_AndRoundTrip()
      {
         string path = Path.Combine(Path.GetTempPath(), $"pkbf-{Guid.NewGuid():N}.pkbf");
         try
         {
            const ulong capacity = 1_000;
            (ulong bits, int k) = BloomSizing.For(capacity, 0.01);

            List<byte[]> inserted = [];
            using (HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bits, k))
            {
               for (int i = 0; i < 200; i++)
               {
                  byte[] sha1 = SHA1.HashData(Encoding.UTF8.GetBytes($"pwd-{i}"));
                  writable.Add(sha1);
                  inserted.Add(sha1);
               }

               writable.CommitHeader();
            }

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            _ = readable.InsertedCount.Should().Be(200);
            _ = readable.SourceTag.Should().Be(HibpBloomFile.DefaultSourceTag);

            foreach (byte[] sha1 in inserted)
            {
               _ = readable.MightContain(sha1).Should().BeTrue();
            }
         }
         finally
         {
            if (File.Exists(path))
            {
               File.Delete(path);
            }
         }
      }

      [TestMethod]
      public void Case03_FalsePositiveRate_IsBounded()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            const ulong capacity = 2_000;
            const double targetFpr = 0.01;
            (ulong bits, int k) = BloomSizing.For(capacity, targetFpr);

            using (HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bits, k))
            {
               for (int i = 0; i < 1_000; i++)
               {
                  writable.Add(SHA1.HashData(Encoding.UTF8.GetBytes($"in-{i}")));
               }

               writable.CommitHeader();
            }

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            int probes = 5_000;
            int hits = 0;
            for (int i = 0; i < probes; i++)
            {
               if (readable.MightContain(SHA1.HashData(Encoding.UTF8.GetBytes($"out-{i}"))))
               {
                  hits++;
               }
            }

            double observed = hits / (double)probes;
            // Allow slack for small sample / sizing rounding.
            _ = observed.Should().BeLessThan(0.05);
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * "test" is in the HIBP corpus. A filter that ingested its SHA-1 must
       * report a hit after close/reopen (no false negatives).
      */
      public void Case04_PasswordTest_IsAHitAfterRoundTrip()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            _ = readable.InsertedCount.Should().Be(1);
            _ = readable.MightContain(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword)).Should().BeTrue();
            _ = readable.MightContain(BloomTestHelper.Sha1("this-password-is-not-in-this-tiny-filter")).Should().BeFalse();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * A copied / compact .pkbf is often the exact logical size, not rounded
       * up to the MMF allocation granularity. Open must still map it.
      */
      public void Case05_Open_ExactLogicalSize_StillFindsTest()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            const ulong capacity = 100;
            const ulong bitCount = 8_000;
            const int hashFunctions = 3;
            long logicalBytes = HibpBloomFile.HeaderSize + (long)((bitCount + 7UL) / 8UL);

            using (HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bitCount, hashFunctions))
            {
               writable.Add(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword));
               writable.CommitHeader();
            }

            using (FileStream trim = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
               trim.SetLength(logicalBytes);
            }

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            _ = readable.MightContain(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword)).Should().BeTrue();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * HibpBloomBuilder disposes the writable filter, then File.Move's the
       * .building file into place. Open of the moved file must still query.
      */
      public void Case06_BuilderCloseAndMove_StillFindsTest()
      {
         string outputPath = BloomTestHelper.TempPkbfPath();
         string buildingPath = outputPath + ".building";
         try
         {
            using (HibpBloomFile writable = HibpBloomFile.Create(buildingPath, 1_000, 16_000, 4))
            {
               writable.Add(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword));
               writable.CommitHeader();
            }

            File.Move(buildingPath, outputPath);

            using HibpBloomFile readable = HibpBloomFile.Open(outputPath);
            _ = readable.MightContain(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword)).Should().BeTrue();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(buildingPath);
            BloomTestHelper.DeleteQuietly(outputPath);
         }
      }

      [TestMethod]
      public void Case07_Open_RejectsCorruptOrTruncatedFiles()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            File.WriteAllBytes(path, "NOTA"u8.ToArray());
            Action badMagic = () => HibpBloomFile.Open(path).Dispose();
            _ = badMagic.Should().Throw<InvalidDataException>();

            File.WriteAllBytes(path, new byte[10]);
            Action tooSmall = () => HibpBloomFile.Open(path).Dispose();
            _ = tooSmall.Should().Throw<InvalidDataException>();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      public void Case08_ReadOnlyOpen_CannotAdd()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            Action add = () => readable.Add(BloomTestHelper.Sha1("other"));
            _ = add.Should().Throw<InvalidOperationException>();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * LeakFilterConfig is what PasswordFactory uses to attach the on-disk
       * filter. Opening the configured path must yield a live membership probe.
      */
      public void Case09_LeakFilterConfig_OpensConfiguredFilter()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            LeakFilterConfig disabled = new() { Enabled = false, FilterPath = path };
            _ = disabled.TryOpenConfiguredFilter().Should().BeNull();

            LeakFilterConfig missing = new() { Enabled = true, FilterPath = path + ".missing" };
            _ = missing.TryOpenConfiguredFilter().Should().BeNull();

            LeakFilterConfig enabled = new() { Enabled = true, FilterPath = path };
            using ILocalLeakFilter? filter = enabled.TryOpenConfiguredFilter();
            _ = filter.Should().NotBeNull();
            _ = filter!.MightContain(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword)).Should().BeTrue();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }
   }
}
