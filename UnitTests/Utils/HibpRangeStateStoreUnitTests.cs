using FluentAssertions;
using System.Text;
using Upsilon.Apps.Passkey.Utils.LeakFilter;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   /// <summary>
   /// Covers the incremental refresh machinery: in-place insertion into an existing
   /// filter, and the sidecar that decides which ranges may be skipped.
   /// </summary>
   [TestClass]
   public sealed class HibpRangeStateStoreUnitTests
   {
      private const string HIBP_ETAG = "\"0x8DED132F654ED41\"";
      private const int PREFIX_COUNT = 16;

      [TestMethod]
      /*
       * A refresh folds new ranges into the filter already on disk. Bloom filters
       * are closed under union, so nothing previously inserted may be lost.
      */
      public void Case01_OpenForUpdate_KeepsEarlierHashesAndAddsNewOnes()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using (HibpBloomFile update = HibpBloomFile.OpenForUpdate(path))
            {
               update.Add(BloomTestHelper.Sha1("added-by-the-refresh"));
               update.CommitHeader();
            }

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            _ = readable.InsertedCount.Should().Be(2);
            _ = readable.MightContain(BloomTestHelper.Sha1(BloomTestHelper.LeakedPassword)).Should().BeTrue();
            _ = readable.MightContain(BloomTestHelper.Sha1("added-by-the-refresh")).Should().BeTrue();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * Ingestion runs one task per range and two ranges routinely touch two bits
       * of the same byte. A lost read-modify-write would be a false negative on a
       * leak check, so every hash must still be found afterwards.
      */
      public void Case02_ConcurrentAdd_LosesNoHash()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            const ulong capacity = 20_000;
            (ulong bits, int hashFunctions) = BloomSizing.For(capacity, 0.01);
            byte[][] hashes = [.. Enumerable.Range(0, 5_000).Select(i => BloomTestHelper.Sha1($"concurrent-{i}"))];

            using (HibpBloomFile writable = HibpBloomFile.Create(path, capacity, bits, hashFunctions))
            {
               _ = Parallel.ForEach(
                  hashes,
                  new ParallelOptions { MaxDegreeOfParallelism = 16 },
                  hash => writable.Add(hash));

               writable.CommitHeader();
            }

            using HibpBloomFile readable = HibpBloomFile.Open(path);
            _ = readable.InsertedCount.Should().Be((ulong)hashes.Length);

            foreach (byte[] hash in hashes)
            {
               _ = readable.MightContain(hash).Should().BeTrue();
            }
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * The whole point of the sidecar: an ETag recorded during one run must come
       * back on the next one, so the range can be revalidated instead of fetched.
      */
      public void Case03_RecordedEtags_SurviveReopen()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.MarkIngested(3, HIBP_ETAG);
               store.MarkIngested(7, etag: null);
               _ = store.PendingCount.Should().Be(2);

               store.Commit(filter);
               _ = store.PendingCount.Should().Be(0);
               _ = store.IngestedPrefixes.Should().Be(2);
            }

            using HibpBloomFile reopened = HibpBloomFile.OpenForUpdate(path);
            using HibpRangeStateStore? resumed = HibpRangeStateStore.TryOpen(statePath, PREFIX_COUNT, reopened);

            _ = resumed.Should().NotBeNull();
            _ = resumed!.IngestedPrefixes.Should().Be(2);

            _ = resumed.TryGetIngested(3, out string? withEtag).Should().BeTrue();
            _ = withEtag.Should().Be(HIBP_ETAG);

            // The API answered without an ETag: the range counts as ingested but
            // cannot be revalidated, so it will be fetched unconditionally.
            _ = resumed.TryGetIngested(7, out string? withoutEtag).Should().BeTrue();
            _ = withoutEtag.Should().BeNull();

            _ = resumed.TryGetIngested(4, out string? untouched).Should().BeFalse();
            _ = untouched.Should().BeNull();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * A sidecar that outlived its filter must never be trusted: skipping ranges
       * whose bits are absent would silently report leaked passwords as clean.
      */
      public void Case04_SidecarIsRejected_WhenTheFilterMovedOn()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.MarkIngested(1, HIBP_ETAG);
               store.Commit(filter);
            }

            // Stands in for anything that changes the filter behind the sidecar's
            // back: a rebuild, a restore from backup, a manual delete.
            using (HibpBloomFile diverged = HibpBloomFile.OpenForUpdate(path))
            {
               diverged.Add(BloomTestHelper.Sha1("written-without-the-sidecar"));
               diverged.CommitHeader();
            }

            using HibpBloomFile reopened = HibpBloomFile.OpenForUpdate(path);
            _ = HibpRangeStateStore.TryOpen(statePath, PREFIX_COUNT, reopened).Should().BeNull();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * Bit positions depend on the sizing, so a sidecar written against other
       * parameters describes ranges this filter never absorbed.
      */
      public void Case05_SidecarIsRejected_OnSizingAndPrefixCountMismatch()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string otherPath = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.Commit(filter);
            }

            using HibpBloomFile sameFilter = HibpBloomFile.OpenForUpdate(path);
            _ = HibpRangeStateStore.TryOpen(statePath, PREFIX_COUNT * 2, sameFilter).Should().BeNull();

            using (HibpBloomFile differentSizing = HibpBloomFile.Create(otherPath, 500, 9_000, 3))
            {
               differentSizing.CommitHeader();
            }

            using HibpBloomFile otherFilter = HibpBloomFile.OpenForUpdate(otherPath);
            _ = HibpRangeStateStore.TryOpen(statePath, PREFIX_COUNT, otherFilter).Should().BeNull();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(otherPath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * Closing a filter that gained nothing since its last checkpoint must not
       * restamp the header, otherwise every close would invalidate the sidecar.
      */
      public void Case06_DisposeWithoutInserts_KeepsTheSidecarValid()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            long stampedTicks;
            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.MarkIngested(2, HIBP_ETAG);
               store.Commit(filter);
               stampedTicks = filter.LastStamp.BuiltUtcTicks;
            }

            using HibpBloomFile reopened = HibpBloomFile.OpenForUpdate(path);
            _ = reopened.LastStamp.BuiltUtcTicks.Should().Be(stampedTicks);

            using HibpRangeStateStore? stillValid = HibpRangeStateStore.TryOpen(statePath, PREFIX_COUNT, reopened);
            _ = stillValid.Should().NotBeNull();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * An ETag too long for the fixed-width record is dropped rather than
       * truncated: a truncated tag would match nothing and, worse, could be
       * replayed as a different range's tag.
      */
      public void Case07_OversizedEtag_IsNotRecorded()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path);
            using HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter);

            store.MarkIngested(0, new string('a', HibpRangeStateStore.MaxEtagLength + 1));
            _ = store.TryGetIngested(0, out string? dropped).Should().BeTrue();
            _ = dropped.Should().BeNull();

            string longestAccepted = new('b', HibpRangeStateStore.MaxEtagLength);
            store.MarkIngested(1, longestAccepted);
            _ = store.TryGetIngested(1, out string? kept).Should().BeTrue();
            _ = kept.Should().Be(longestAccepted);
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * Deleting the offline database has to take the sidecar with it, otherwise
       * tens of megabytes survive a user-visible "delete".
      */
      public void Case08_DeletingTheFilter_AlsoDeletesTheSidecar()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.Commit(filter);
            }

            _ = File.Exists(statePath).Should().BeTrue();

            LeakFilterConfig config = new(path) { Enabled = true, };
            _ = config.TryDeleteFilterFile().Should().BeTrue();

            _ = File.Exists(path).Should().BeFalse();
            _ = File.Exists(statePath).Should().BeFalse();
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * BuildIfMissing must never touch the network when a filter is present: it
       * is the mode the app uses on paths where a multi-hour download would be a
       * surprise.
      */
      public async Task Case09_BuildIfMissing_SkipsAnExistingFilterWithoutDownloading()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            HibpBloomBuildResult result = await HibpBloomBuilder
               .RunAsync(path, HibpBloomBuildMode.BuildIfMissing)
               .ConfigureAwait(false);

            _ = result.Skipped.Should().BeTrue();
            _ = result.IsRefresh.Should().BeFalse();
            _ = result.DownloadedBytes.Should().Be(0);
            _ = result.OutputPath.Should().Be(path);
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * A filter still mapped by a running leak check shares it read-only, which
       * denies the read-write handle a refresh needs. That has to surface: silently
       * treating it as "unusable" would turn a locked file into a full corpus
       * download. The tiny sizing and the timeout keep a regression cheap.
      */
      public async Task Case10_Update_FailsLoudlyWhileTheFilterIsStillMapped()
      {
         string path = BloomTestHelper.TempPkbfPath();
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            LeakFilterConfig config = new(path) { Enabled = true, };
            using ILocalLeakFilter? attached = config.TryOpenConfiguredFilter();
            _ = attached.Should().NotBeNull();

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            Func<Task> refresh = () => HibpBloomBuilder.RunAsync(
               path,
               HibpBloomBuildMode.Update,
               capacity: 1_000,
               falsePositiveRate: 0.01,
               maxDegreeOfParallelism: 1,
               cancellationToken: timeout.Token);

            _ = await refresh.Should().ThrowAsync<IOException>().ConfigureAwait(false);
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(HibpRangeStateStore.PathFor(path + ".building"));
            BloomTestHelper.DeleteQuietly(path + ".building");
            BloomTestHelper.DeleteQuietly(HibpRangeStateStore.PathFor(path));
            BloomTestHelper.DeleteQuietly(path);
         }
      }

      [TestMethod]
      /*
       * The sidecar layout is frozen for the same reason as the .pkbf: a moved
       * field would make a valid sidecar read as ranges it never recorded. Offsets
       * are literals here on purpose — this test is the format specification, so it
       * must not share the constants it is meant to pin down.
      */
      public void Case11_SidecarByteLayout_IsFrozen()
      {
         string path = BloomTestHelper.TempPkbfPath();
         string statePath = HibpRangeStateStore.PathFor(path);
         try
         {
            BloomTestHelper.WriteBloomContaining(path, BloomTestHelper.LeakedPassword);

            const int markedPrefix = 5;
            ulong capacity;
            ulong bitCount;
            int hashFunctions;
            HibpBloomStamp stamp;

            using (HibpBloomFile filter = HibpBloomFile.OpenForUpdate(path))
            using (HibpRangeStateStore store = HibpRangeStateStore.CreateNew(statePath, PREFIX_COUNT, filter))
            {
               store.MarkIngested(markedPrefix, HIBP_ETAG);
               store.Commit(filter);

               capacity = filter.Capacity;
               bitCount = filter.BitCount;
               hashFunctions = filter.HashFunctions;
               stamp = filter.LastStamp;
            }

            byte[] raw = File.ReadAllBytes(statePath);

            _ = HibpRangeStateStore.HeaderSize.Should().Be(64);
            _ = HibpRangeStateStore.EntryStride.Should().Be(32);
            _ = HibpRangeStateStore.MaxEtagLength.Should().Be(30);
            _ = raw.Length.Should().Be(64 + (PREFIX_COUNT * 32));

            _ = Encoding.ASCII.GetString(raw, 0, 4).Should().Be("PKRS");
            _ = BitConverter.ToUInt32(raw, 4).Should().Be(1);
            _ = BitConverter.ToInt32(raw, 8).Should().Be(32);
            _ = BitConverter.ToInt32(raw, 12).Should().Be(PREFIX_COUNT);
            _ = BitConverter.ToUInt64(raw, 16).Should().Be(capacity);
            _ = BitConverter.ToUInt64(raw, 24).Should().Be(bitCount);
            _ = BitConverter.ToInt32(raw, 32).Should().Be(hashFunctions);
            _ = BitConverter.ToUInt64(raw, 40).Should().Be(stamp.InsertedCount);
            _ = BitConverter.ToInt64(raw, 48).Should().Be(stamp.BuiltUtcTicks);

            int entry = 64 + (markedPrefix * 32);
            _ = raw[entry].Should().Be(1, "the record is marked ingested");
            _ = raw[entry + 1].Should().Be((byte)HIBP_ETAG.Length);
            _ = Encoding.ASCII.GetString(raw, entry + 2, HIBP_ETAG.Length).Should().Be(HIBP_ETAG);

            int untouched = 64 + ((markedPrefix + 1) * 32);
            _ = raw[untouched..(untouched + 32)].Should().OnlyContain(b => b == 0);
         }
         finally
         {
            BloomTestHelper.DeleteQuietly(statePath);
            BloomTestHelper.DeleteQuietly(path);
         }
      }
   }
}
