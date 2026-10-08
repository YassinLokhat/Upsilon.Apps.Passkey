using FluentAssertions;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Utils
{
   [TestClass]
   public sealed class LeakFilterQualityUnitTests
   {
      [TestMethod]
      public void Case01_RateFromCode_MapsPresetsAndFallsBackToBalanced()
      {
         _ = LeakFilterQuality.RateFromCode(LeakFilterQuality.BalancedCode).Should().Be(LeakFilterQuality.BalancedRate);
         _ = LeakFilterQuality.RateFromCode(LeakFilterQuality.StrictCode).Should().Be(LeakFilterQuality.StrictRate);
         _ = LeakFilterQuality.RateFromCode(LeakFilterQuality.ParanoidCode).Should().Be(LeakFilterQuality.ParanoidRate);
         _ = LeakFilterQuality.RateFromCode("strict").Should().Be(LeakFilterQuality.StrictRate);
         _ = LeakFilterQuality.RateFromCode(null).Should().Be(LeakFilterQuality.BalancedRate);
         _ = LeakFilterQuality.RateFromCode(string.Empty).Should().Be(LeakFilterQuality.BalancedRate);
         _ = LeakFilterQuality.RateFromCode("not-a-preset").Should().Be(LeakFilterQuality.BalancedRate);
      }

      [TestMethod]
      public void Case02_CodeFromRate_MapsExactPresetsAndFallsBackToBalanced()
      {
         _ = LeakFilterQuality.CodeFromRate(LeakFilterQuality.BalancedRate).Should().Be(LeakFilterQuality.BalancedCode);
         _ = LeakFilterQuality.CodeFromRate(LeakFilterQuality.StrictRate).Should().Be(LeakFilterQuality.StrictCode);
         _ = LeakFilterQuality.CodeFromRate(LeakFilterQuality.ParanoidRate).Should().Be(LeakFilterQuality.ParanoidCode);
         _ = LeakFilterQuality.CodeFromRate(0.05).Should().Be(LeakFilterQuality.BalancedCode);
      }

      [TestMethod]
      public void Case03_ApproximateSizeGiB_GrowsAsFprTightens()
      {
         double balanced = LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.BalancedRate);
         double strict = LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.StrictRate);
         double paranoid = LeakFilterQuality.ApproximateSizeGiB(LeakFilterQuality.ParanoidRate);

         _ = balanced.Should().BeApproximately(2.4, 0.2);
         _ = strict.Should().BeGreaterThan(balanced);
         _ = paranoid.Should().BeGreaterThan(strict);
      }

      [TestMethod]
      public void Case04_FileSizingMatches_DetectsPresetOnDisk()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            _ = LeakFilterQuality
               .FileSizingMatches(path, capacity: 1_000, LeakFilterQuality.BalancedRate)
               .Should().BeTrue();
            _ = LeakFilterQuality
               .FileSizingMatches(path, capacity: 1_000, LeakFilterQuality.StrictRate)
               .Should().BeFalse();
            _ = LeakFilterQuality
               .FileSizingMatches(path + ".missing", capacity: 1_000, LeakFilterQuality.BalancedRate)
               .Should().BeFalse();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      public void Case05_CreateWithStrictRate_UsesStrictBitArraySizing()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            const ulong capacity = 1_000;
            (ulong expectedBits, int expectedK) = BloomSizing.For(capacity, LeakFilterQuality.StrictRate);

            using (HibpBloomFile writable = HibpBloomFile.Create(
               path,
               capacity,
               LeakFilterQuality.StrictRate))
            {
               writable.Add(BloomTestHelper.Ntlm(BloomTestHelper.LeakedPassword));
               writable.CommitHeader();
            }

            _ = HibpBloomFile.TryReadSizing(path, out ulong fileCapacity, out ulong fileBits, out int fileK)
               .Should().BeTrue();
            _ = fileCapacity.Should().Be(capacity);
            _ = fileBits.Should().Be(expectedBits);
            _ = fileK.Should().Be(expectedK);
            _ = LeakFilterQuality
               .FileSizingMatches(path, capacity, LeakFilterQuality.StrictRate)
               .Should().BeTrue();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      public void Case06_LeakFilterConfig_DefaultFalsePositiveRateIsBalanced()
      {
         LeakFilterConfig config = new(BloomTestHelper.TempPkbfPath());
         _ = config.FalsePositiveRate.Should().Be(BloomSizing.DefaultFalsePositiveRate);
         _ = config.FalsePositiveRate.Should().Be(LeakFilterQuality.BalancedRate);
      }

      [TestMethod]
      /*
       * Update with a stricter FPR must not silently rebuild into a larger bit
       * array. Without a sidecar the refresh is skipped and the existing sizing
       * stays Balanced.
       */
      public async Task Case07_Update_WithStricterFpr_KeepsOnDiskSizing()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);
            BloomTestHelper.DeleteQuietly(statePath);

            HibpBloomBuildResult result = await HibpBloomBuilder
               .RunAsync(
                  path,
                  HibpBloomBuildMode.Update,
                  capacity: 1_000,
                  falsePositiveRate: LeakFilterQuality.StrictRate,
                  maxDegreeOfParallelism: 1)
               .ConfigureAwait(false);

            _ = result.Skipped.Should().BeTrue();
            _ = result.IsRefresh.Should().BeTrue();
            _ = LeakFilterQuality
               .FileSizingMatches(path, capacity: 1_000, LeakFilterQuality.BalancedRate)
               .Should().BeTrue();
            _ = LeakFilterQuality
               .FileSizingMatches(path, capacity: 1_000, LeakFilterQuality.StrictRate)
               .Should().BeFalse();
            _ = File.Exists(path + ".building").Should().BeFalse();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(HibpRangeStateStore.PathFor(path + ".building"));
            BloomTestHelper.DeleteQuietly(path + ".building");
            BloomTestHelper.DeleteQuietly(path);
         }
      }
   }
}
