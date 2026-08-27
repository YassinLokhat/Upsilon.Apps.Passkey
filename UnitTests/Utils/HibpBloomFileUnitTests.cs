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
         string path = Path.Combine(Path.GetTempPath(), $"pkbf-{Guid.NewGuid():N}.pkbf");
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
            if (File.Exists(path))
            {
               File.Delete(path);
            }
         }
      }
   }
}
