using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// What <see cref="HibpBloomBuilder.RunAsync"/> should do with the filter that
   /// may already be on disk.
   /// </summary>
   public enum HibpBloomBuildMode
   {
      /// <summary>
      /// Build only when no filter exists, otherwise report a skip.
      /// </summary>
      BuildIfMissing,

      /// <summary>
      /// Revalidate every range of an existing filter and fold in what changed.
      /// Requires a usable <c>.ranges</c> sidecar with recorded ETags; otherwise
      /// the existing filter is kept and the run is reported as skipped (use
      /// <see cref="Rebuild"/> to restore incremental updates). Falls back to a
      /// full build only when the <c>.pkbf</c> itself cannot be refreshed in place.
      /// </summary>
      Update,

      /// <summary>
      /// Download the whole corpus into a new filter, discarding the current one.
      /// </summary>
      Rebuild,
   }

   /// <summary>
   /// Downloads HIBP NTLM ranges into a local <c>.pkbf</c> Bloom filter.
   /// First builds are checkpointed; refreshes use <c>If-None-Match</c> against the <c>.ranges</c> sidecar.
   /// </summary>
   public static class HibpBloomBuilder
   {
      public const int TotalPrefixes = 1 << 20; // 1048576 = 16^5

      /// <summary>Concurrent range requests (default for refresh wall-clock).</summary>
      public const int DefaultParallelism = 64;

      private const string BUILDING_SUFFIX = ".building";
      private const string USER_AGENT = "Upsilon.Apps.Passkey-LeakFilter/1.0";
      private const int MAX_ATTEMPTS = 5;
      private const int PREFIX_HEX_LENGTH = 5;
      private const int SUFFIX_HEX_LENGTH = 27; // NTLM = 32 hex chars total

      /// <summary>Prefixes between checkpoints (bounds resume cost vs flush cost).</summary>
      private const int CHECKPOINT_PREFIXES = 4096;

      private const int PROGRESS_PREFIXES = 256;

      // Never send Add-Padding: the API varies on it and would defeat ETag revalidation.
      private static readonly SocketsHttpHandler _httpHandler = new()
      {
         AutomaticDecompression = DecompressionMethods.All,
         MaxConnectionsPerServer = 256,
         EnableMultipleHttp2Connections = true,
         PooledConnectionLifetime = TimeSpan.FromMinutes(10),
      };

      private static readonly HttpClient _http = _createHttpClient();

      /// <summary>
      /// Builds a filter at <paramref name="outputPath"/>. When <paramref name="force"/>
      /// is false and the file already exists, returns without downloading.
      /// </summary>
      public static Task<HibpBloomBuildResult> BuildAsync(
         string outputPath,
         ulong capacity = BloomSizing.DefaultCapacity,
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate,
         int maxDegreeOfParallelism = DefaultParallelism,
         bool force = false,
         IProgress<HibpBloomBuildProgress>? progress = null,
         CancellationToken cancellationToken = default)
         => RunAsync(
            outputPath,
            force ? HibpBloomBuildMode.Rebuild : HibpBloomBuildMode.BuildIfMissing,
            capacity,
            falsePositiveRate,
            maxDegreeOfParallelism,
            progress,
            cancellationToken);

      /// <summary>
      /// Refreshes the filter at <paramref name="outputPath"/> in place, downloading
      /// only the ranges that changed since the last run.
      /// </summary>
      public static Task<HibpBloomBuildResult> UpdateAsync(
         string outputPath,
         ulong capacity = BloomSizing.DefaultCapacity,
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate,
         int maxDegreeOfParallelism = DefaultParallelism,
         IProgress<HibpBloomBuildProgress>? progress = null,
         CancellationToken cancellationToken = default)
         => RunAsync(
            outputPath,
            HibpBloomBuildMode.Update,
            capacity,
            falsePositiveRate,
            maxDegreeOfParallelism,
            progress,
            cancellationToken);

      /// <summary>
      /// Brings the filter at <paramref name="outputPath"/> to the state requested by
      /// <paramref name="mode"/>.
      /// </summary>
      public static async Task<HibpBloomBuildResult> RunAsync(
         string outputPath,
         HibpBloomBuildMode mode = HibpBloomBuildMode.BuildIfMissing,
         ulong capacity = BloomSizing.DefaultCapacity,
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate,
         int maxDegreeOfParallelism = DefaultParallelism,
         IProgress<HibpBloomBuildProgress>? progress = null,
         CancellationToken cancellationToken = default)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

         if (mode == HibpBloomBuildMode.BuildIfMissing && File.Exists(outputPath))
         {
            HibpBloomBuildResult skipped = new(
               outputPath,
               Skipped: true,
               InsertedCount: 0,
               File.GetLastWriteTimeUtc(outputPath),
               IsRefresh: false,
               UnchangedPrefixes: 0,
               ChangedPrefixes: 0,
               DownloadedBytes: 0);

            progress?.Report(new HibpBloomBuildProgress(
               TotalPrefixes,
               TotalPrefixes,
               InsertedHashes: 0,
               Skipped: true,
               IsRefresh: false,
               UnchangedPrefixes: 0,
               ChangedPrefixes: 0,
               DownloadedBytes: 0));

            return skipped;
         }

         if (mode == HibpBloomBuildMode.Update && File.Exists(outputPath))
         {
            HibpBloomBuildResult? refreshed = await _tryRefreshInPlaceAsync(
               outputPath,
               capacity,
               falsePositiveRate,
               maxDegreeOfParallelism,
               progress,
               cancellationToken).ConfigureAwait(false);

            if (refreshed is not null)
            {
               return refreshed.Value;
            }
         }

         return await _buildFromScratchAsync(
            outputPath,
            capacity,
            falsePositiveRate,
            maxDegreeOfParallelism,
            progress,
            cancellationToken).ConfigureAwait(false);
      }

      /// <summary>
      /// Sidecar path holding the per-range ETags of <paramref name="filterPath"/>.
      /// </summary>
      public static string GetRangeStatePath(string filterPath)
         => HibpRangeStateStore.PathFor(filterPath);

      /// <summary>
      /// Refreshes an existing filter in place, or returns <see langword="null"/>
      /// when it cannot be reused — a corrupt file, or sizing that no longer matches
      /// the requested capacity, both of which need a full build.
      /// </summary>
      private static async Task<HibpBloomBuildResult?> _tryRefreshInPlaceAsync(
         string outputPath,
         ulong capacity,
         double falsePositiveRate,
         int maxDegreeOfParallelism,
         IProgress<HibpBloomBuildProgress>? progress,
         CancellationToken cancellationToken)
      {
         (ulong bitCount, int hashFunctions) = BloomSizing.For(capacity, falsePositiveRate);
         string statePath = GetRangeStatePath(outputPath);

         // Open inside this using so ownership never leaves via return (CA2000 /
         // CodeQL factory transfer). Sharing violations still propagate: silently
         // folding that into a full corpus download would be a terrible trade.
         try
         {
            using HibpBloomFile filter = HibpBloomFile.OpenForUpdate(outputPath);

            // A bit array sized for other parameters cannot absorb these hashes:
            // the positions would not match what a later query computes.
            if (!_sizingMatches(filter, capacity, bitCount, hashFunctions))
            {
               return null;
            }

            // Without a usable sidecar there are no ETags to revalidate against.
            // Creating an empty one and ingesting would re-download every range
            // (~1M requests) while the .pkbf itself is still valid for lookups —
            // that must never happen on Update (especially auto-update at startup).
            // Callers that need a fresh corpus should use Rebuild.
            using HibpRangeStateStore? store = HibpRangeStateStore.TryOpen(statePath, TotalPrefixes, filter);
            if (store is null || store.IngestedPrefixes == 0)
            {
               System.Diagnostics.Trace.TraceWarning(
                  $"HIBP range sidecar missing or empty at '{statePath}'; refresh skipped, existing filter kept.");

               progress?.Report(new HibpBloomBuildProgress(
                  TotalPrefixes,
                  TotalPrefixes,
                  InsertedHashes: 0,
                  Skipped: true,
                  IsRefresh: true,
                  UnchangedPrefixes: 0,
                  ChangedPrefixes: 0,
                  DownloadedBytes: 0));

               return new HibpBloomBuildResult(
                  outputPath,
                  Skipped: true,
                  filter.InsertedCount,
                  filter.BuiltUtc,
                  IsRefresh: true,
                  UnchangedPrefixes: 0,
                  ChangedPrefixes: 0,
                  DownloadedBytes: 0);
            }

            (HibpBloomIngestTotals totals, _) = await _ingestWithCheckpointAsync(
               filter,
               store,
               revalidate: true,
               maxDegreeOfParallelism,
               progress,
               cancellationToken).ConfigureAwait(false);

            return new HibpBloomBuildResult(
               outputPath,
               Skipped: false,
               filter.InsertedCount,
               filter.BuiltUtc,
               IsRefresh: true,
               totals.UnchangedPrefixes,
               totals.ChangedPrefixes,
               totals.DownloadedBytes);
         }
         catch (InvalidDataException ex)
         {
            System.Diagnostics.Trace.TraceWarning($"Bloom filter at '{outputPath}' is unusable and will be rebuilt: {ex}");
            return null;
         }
      }

      private static async Task<HibpBloomBuildResult> _buildFromScratchAsync(
         string outputPath,
         ulong capacity,
         double falsePositiveRate,
         int maxDegreeOfParallelism,
         IProgress<HibpBloomBuildProgress>? progress,
         CancellationToken cancellationToken)
      {
         string? directory = Path.GetDirectoryName(outputPath);
         if (!string.IsNullOrEmpty(directory))
         {
            _ = Directory.CreateDirectory(directory);
         }

         string tempPath = outputPath + BUILDING_SUFFIX;
         string tempStatePath = GetRangeStatePath(tempPath);
         (ulong bitCount, int hashFunctions) = BloomSizing.For(capacity, falsePositiveRate);

         HibpBloomIngestTotals totals = default;
         ulong insertedCount = 0;
         bool resumed = false;

         // Resume only when the in-progress filter and sidecar still agree. Open /
         // create stay inside using scopes here — no factory returns a live handle.
         if (File.Exists(tempPath))
         {
            try
            {
               using HibpBloomFile filter = HibpBloomFile.OpenForUpdate(tempPath);
               if (_sizingMatches(filter, capacity, bitCount, hashFunctions))
               {
                  using HibpRangeStateStore? store = HibpRangeStateStore.TryOpen(tempStatePath, TotalPrefixes, filter);
                  if (store is not null)
                  {
                     (totals, insertedCount) = await _ingestWithCheckpointAsync(
                        filter,
                        store,
                        revalidate: false,
                        maxDegreeOfParallelism,
                        progress,
                        cancellationToken).ConfigureAwait(false);
                     resumed = true;
                  }
               }
            }
            catch (InvalidDataException ex)
            {
               System.Diagnostics.Trace.TraceWarning($"Bloom filter at '{tempPath}' is unusable and will be rebuilt: {ex}");
            }
            catch (Exception ex)
               when (ex is IOException
               or UnauthorizedAccessException
               or NotSupportedException
               or ArgumentException
               or System.Security.SecurityException)
            {
               System.Diagnostics.Trace.TraceWarning($"In-progress build at '{tempPath}' cannot be resumed, restarting it: {ex}");
            }
         }

         if (!resumed)
         {
            _deleteQuietly(tempPath);
            _deleteQuietly(tempStatePath);

            using HibpBloomFile filter = HibpBloomFile.Create(tempPath, capacity, falsePositiveRate);
            using HibpRangeStateStore store = HibpRangeStateStore.CreateNew(tempStatePath, TotalPrefixes, filter);
            (totals, insertedCount) = await _ingestWithCheckpointAsync(
               filter,
               store,
               revalidate: false,
               maxDegreeOfParallelism,
               progress,
               cancellationToken).ConfigureAwait(false);
         }

         _deleteQuietly(outputPath);
         File.Move(tempPath, outputPath);
         _deleteQuietly(GetRangeStatePath(outputPath));
         File.Move(GetRangeStatePath(tempPath), GetRangeStatePath(outputPath));

         return new HibpBloomBuildResult(
            outputPath,
            Skipped: false,
            insertedCount,
            File.GetLastWriteTimeUtc(outputPath),
            IsRefresh: false,
            totals.UnchangedPrefixes,
            totals.ChangedPrefixes,
            totals.DownloadedBytes);
      }

      private static bool _sizingMatches(HibpBloomFile filter, ulong capacity, ulong bitCount, int hashFunctions)
         => filter.Capacity == capacity
            && filter.BitCount == bitCount
            && filter.HashFunctions == hashFunctions;

      /// <summary>
      /// Runs one full ingest pass and commits. On cancel/failure the partial pair
      /// is still checkpointed so the next run can resume.
      /// </summary>
      private static async Task<(HibpBloomIngestTotals Totals, ulong InsertedCount)> _ingestWithCheckpointAsync(
         HibpBloomFile filter,
         HibpRangeStateStore store,
         bool revalidate,
         int maxDegreeOfParallelism,
         IProgress<HibpBloomBuildProgress>? progress,
         CancellationToken cancellationToken)
      {
         bool committed = false;
         try
         {
            HibpBloomIngestTotals totals = await _ingestAllAsync(
               filter,
               store,
               revalidate,
               maxDegreeOfParallelism,
               progress,
               cancellationToken).ConfigureAwait(false);

            store.Commit(filter);
            committed = true;
            return (totals, filter.InsertedCount);
         }
         finally
         {
            if (!committed)
            {
               _commitQuietly(filter, store);
            }
         }
      }

      /// <summary>
      /// Walks every prefix once. With <paramref name="revalidate"/> a known range is
      /// re-requested conditionally and only re-ingested on a body; without it, a
      /// range already recorded as folded in is skipped outright, which is how an
      /// interrupted build resumes.
      /// </summary>
      private static async Task<HibpBloomIngestTotals> _ingestAllAsync(
         HibpBloomFile filter,
         HibpRangeStateStore store,
         bool revalidate,
         int maxDegreeOfParallelism,
         IProgress<HibpBloomBuildProgress>? progress,
         CancellationToken cancellationToken)
      {
         long completed = 0;
         long unchanged = 0;
         long changed = 0;
         long inserted = 0;
         long downloadedBytes = 0;
         long sinceCheckpoint = 0;
         System.Threading.Lock checkpointGate = new();

         await Parallel.ForEachAsync(
            Enumerable.Range(0, TotalPrefixes),
            new ParallelOptions
            {
               MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism),
               CancellationToken = cancellationToken,
            },
            async (index, token) =>
            {
               bool known = store.TryGetIngested(index, out string? knownEtag);

               if (known && !revalidate)
               {
                  _ = Interlocked.Increment(ref unchanged);
               }
               else
               {
                  string prefix = index.ToString("X5", CultureInfo.InvariantCulture);
                  HibpRangeFetch fetch = await _fetchRangeAsync(
                     prefix,
                     known ? knownEtag : null,
                     token).ConfigureAwait(false);

                  if (fetch.NotModified)
                  {
                     _ = Interlocked.Increment(ref unchanged);
                  }
                  else
                  {
                     _ = Interlocked.Add(ref inserted, _ingestRange(filter, prefix, fetch.Body));
                     _ = Interlocked.Add(ref downloadedBytes, fetch.Body.Length);
                     _ = Interlocked.Increment(ref changed);
                     store.MarkIngested(index, fetch.ETag);
                  }
               }

               long done = Interlocked.Increment(ref completed);

               if (Interlocked.Increment(ref sinceCheckpoint) >= CHECKPOINT_PREFIXES)
               {
                  _checkpoint(filter, store, checkpointGate, ref sinceCheckpoint);
               }

               if (done % PROGRESS_PREFIXES == 0 || done == TotalPrefixes)
               {
                  progress?.Report(new HibpBloomBuildProgress(
                     (int)done,
                     TotalPrefixes,
                     Interlocked.Read(ref inserted),
                     Skipped: false,
                     revalidate,
                     (int)Interlocked.Read(ref unchanged),
                     (int)Interlocked.Read(ref changed),
                     Interlocked.Read(ref downloadedBytes)));
               }
            }).ConfigureAwait(false);

         return new HibpBloomIngestTotals(
            Interlocked.Read(ref inserted),
            (int)Interlocked.Read(ref unchanged),
            (int)Interlocked.Read(ref changed),
            Interlocked.Read(ref downloadedBytes));
      }

      /// <summary>
      /// Kept out of the async ingestion body on purpose: the flush chain is
      /// synchronous by nature, and one task at a time is enough.
      /// </summary>
      private static void _checkpoint(
         HibpBloomFile filter,
         HibpRangeStateStore store,
         System.Threading.Lock gate,
         ref long sinceCheckpoint)
      {
         lock (gate)
         {
            if (Interlocked.Read(ref sinceCheckpoint) < CHECKPOINT_PREFIXES)
            {
               return;
            }

            _ = Interlocked.Exchange(ref sinceCheckpoint, 0);
            store.Commit(filter);
         }
      }

      private static void _commitQuietly(HibpBloomFile filter, HibpRangeStateStore store)
      {
         try
         {
            store.Commit(filter);
         }
         catch (Exception ex)
            when (ex is IOException
            or ObjectDisposedException
            or UnauthorizedAccessException
            or NotSupportedException)
         {
            System.Diagnostics.Trace.TraceWarning($"Range checkpoint failed, the next run will re-fetch more ranges: {ex}");
         }
      }

      private static async Task<HibpRangeFetch> _fetchRangeAsync(
         string prefix,
         string? knownEtag,
         CancellationToken cancellationToken)
      {
         Uri uri = new($"https://api.pwnedpasswords.com/range/{prefix}?mode=ntlm");

         for (int attempt = 1; ; attempt++)
         {
            try
            {
               using HttpRequestMessage request = new(HttpMethod.Get, uri);
               if (!string.IsNullOrEmpty(knownEtag))
               {
                  // Replayed verbatim, quotes and any weak marker included.
                  _ = request.Headers.TryAddWithoutValidation("If-None-Match", knownEtag);
               }

               using HttpResponseMessage response = await _http
                  .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                  .ConfigureAwait(false);

               if (response.StatusCode == HttpStatusCode.NotModified)
               {
                  return new HibpRangeFetch(NotModified: true, string.Empty, knownEtag);
               }

               _ = response.EnsureSuccessStatusCode();
               string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
               return new HibpRangeFetch(NotModified: false, body, _cacheableEtag(response));
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < MAX_ATTEMPTS)
            {
               await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
            }
         }
      }

      private static string? _cacheableEtag(HttpResponseMessage response)
      {
         EntityTagHeaderValue? tag = response.Headers.ETag;
         if (tag is null)
         {
            return null;
         }

         // A tag too long for the sidecar is simply not recorded: that range then
         // costs one unconditional download on the next refresh.
         string value = tag.ToString();
         return value.Length is > 0 and <= HibpRangeStateStore.MaxEtagLength ? value : null;
      }

      private static int _ingestRange(HibpBloomFile filter, ReadOnlySpan<char> prefix, string body)
      {
         Span<char> hex = stackalloc char[PREFIX_HEX_LENGTH + SUFFIX_HEX_LENGTH];
         prefix.CopyTo(hex);
         Span<byte> ntlm = stackalloc byte[HibpBloomFile.NtlmByteLength];

         int added = 0;
         foreach (ReadOnlySpan<char> rawLine in body.AsSpan().EnumerateLines())
         {
            ReadOnlySpan<char> line = rawLine.Trim();
            int separator = line.IndexOf(':');
            ReadOnlySpan<char> suffix = separator >= 0 ? line[..separator] : line;
            if (suffix.Length != SUFFIX_HEX_LENGTH)
            {
               continue;
            }

            suffix.CopyTo(hex[PREFIX_HEX_LENGTH..]);
            if (Convert.FromHexString(hex, ntlm, out _, out int written) != OperationStatus.Done
               || written != HibpBloomFile.NtlmByteLength)
            {
               continue;
            }

            filter.Add(ntlm);
            added++;
         }

         return added;
      }

      private static void _deleteQuietly(string path)
      {
         if (File.Exists(path))
         {
            File.Delete(path);
         }
      }

      private static HttpClient _createHttpClient()
      {
         HttpClient client = new(_httpHandler)
         {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
         };

         client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
         return client;
      }

      private readonly record struct HibpRangeFetch(bool NotModified, string Body, string? ETag);
   }

   /// <summary>
   /// Progress snapshot while building or refreshing a HIBP Bloom filter.
   /// </summary>
   public readonly record struct HibpBloomBuildProgress(
      int CompletedPrefixes,
      int TotalPrefixes,
      long InsertedHashes,
      bool Skipped,
      bool IsRefresh,
      int UnchangedPrefixes,
      int ChangedPrefixes,
      long DownloadedBytes);

   /// <summary>
   /// Result of <see cref="HibpBloomBuilder.RunAsync"/>.
   /// </summary>
   public readonly record struct HibpBloomBuildResult(
      string OutputPath,
      bool Skipped,
      ulong InsertedCount,
      DateTime BuiltUtc,
      bool IsRefresh,
      int UnchangedPrefixes,
      int ChangedPrefixes,
      long DownloadedBytes);

   /// <summary>
   /// Counters accumulated by one pass over the range space.
   /// </summary>
   internal readonly record struct HibpBloomIngestTotals(
      long InsertedHashes,
      int UnchangedPrefixes,
      int ChangedPrefixes,
      long DownloadedBytes);
}
