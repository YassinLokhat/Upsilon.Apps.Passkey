using System.Globalization;

namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Downloads HIBP SHA-1 ranges and builds a local <c>.pkbf</c> Bloom filter.
   /// Long-running (hours for a full corpus); intended for the LeakFilterBuilder tool / UI.
   /// </summary>
   public static class HibpBloomBuilder
   {
      public const int TotalPrefixes = 1 << 20; // 1048576 = 16^5

      private static readonly HttpClient _http = new()
      {
         Timeout = TimeSpan.FromSeconds(60),
      };

      /// <summary>
      /// Builds a filter at <paramref name="outputPath"/>. When <paramref name="force"/> is
      /// false and the file already exists, returns without downloading.
      /// </summary>
      public static async Task<HibpBloomBuildResult> BuildAsync(
         string outputPath,
         ulong capacity = BloomSizing.DefaultCapacity,
         double falsePositiveRate = BloomSizing.DefaultFalsePositiveRate,
         int maxDegreeOfParallelism = 32,
         bool force = false,
         IProgress<HibpBloomBuildProgress>? progress = null,
         CancellationToken cancellationToken = default)
      {
         ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

         if (!force && File.Exists(outputPath))
         {
            progress?.Report(new HibpBloomBuildProgress(TotalPrefixes, TotalPrefixes, 0, Skipped: true));
            return new HibpBloomBuildResult(outputPath, Skipped: true, InsertedCount: 0, BuiltUtc: File.GetLastWriteTimeUtc(outputPath));
         }

         string? directory = Path.GetDirectoryName(outputPath);
         if (!string.IsNullOrEmpty(directory))
         {
            _ = Directory.CreateDirectory(directory);
         }

         string tempPath = outputPath + ".building";
         if (File.Exists(tempPath))
         {
            File.Delete(tempPath);
         }

         HibpBloomFile filter = HibpBloomFile.Create(tempPath, capacity, falsePositiveRate);
         object addGate = new();
         long completedPrefixes = 0;
         long inserted = 0;

         try
         {
            await Parallel.ForEachAsync(
               Enumerable.Range(0, TotalPrefixes),
               new ParallelOptions
               {
                  MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism),
                  CancellationToken = cancellationToken,
               },
               async (index, token) =>
               {
                  string prefix = index.ToString("X5", CultureInfo.InvariantCulture);
                  string body = await _downloadRangeAsync(prefix, token).ConfigureAwait(false);
                  int added = _ingestRange(filter, addGate, prefix, body);
                  long done = Interlocked.Increment(ref completedPrefixes);
                  long totalInserted = Interlocked.Add(ref inserted, added);
                  if (done % 256 == 0 || done == TotalPrefixes)
                  {
                     progress?.Report(new HibpBloomBuildProgress((int)done, TotalPrefixes, totalInserted, Skipped: false));
                  }
               }).ConfigureAwait(false);

            filter.CommitHeader();
         }
         finally
         {
            filter.Dispose();
         }

         if (File.Exists(outputPath))
         {
            File.Delete(outputPath);
         }

         File.Move(tempPath, outputPath);

         DateTime builtUtc = File.GetLastWriteTimeUtc(outputPath);
         progress?.Report(new HibpBloomBuildProgress(TotalPrefixes, TotalPrefixes, inserted, Skipped: false));
         return new HibpBloomBuildResult(outputPath, Skipped: false, (ulong)inserted, builtUtc);
      }

      private static async Task<string> _downloadRangeAsync(string prefix, CancellationToken cancellationToken)
      {
         Uri uri = new($"https://api.pwnedpasswords.com/range/{prefix}");
         const int maxAttempts = 5;
         for (int attempt = 1; attempt <= maxAttempts; attempt++)
         {
            try
            {
               using HttpResponseMessage response = await _http
                  .GetAsync(uri, cancellationToken)
                  .ConfigureAwait(false);
               _ = response.EnsureSuccessStatusCode();
               return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < maxAttempts)
            {
               await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
            }
         }

         using HttpResponseMessage last = await _http
            .GetAsync(uri, cancellationToken)
            .ConfigureAwait(false);
         _ = last.EnsureSuccessStatusCode();
         return await last.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      }

      private static int _ingestRange(HibpBloomFile filter, object addGate, string prefix, string body)
      {
         int added = 0;
         foreach (string line in body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
         {
            int separator = line.IndexOf(':', StringComparison.Ordinal);
            string suffix = separator >= 0 ? line[..separator] : line;
            if (suffix.Length == 0)
            {
               continue;
            }

            string hex = prefix + suffix;
            if (hex.Length != 40)
            {
               continue;
            }

            byte[] sha1 = Convert.FromHexString(hex);
            lock (addGate)
            {
               filter.Add(sha1);
            }

            added++;
         }

         return added;
      }
   }

   /// <summary>
   /// Progress snapshot while building a full HIBP Bloom filter.
   /// </summary>
   public readonly record struct HibpBloomBuildProgress(
      int CompletedPrefixes,
      int TotalPrefixes,
      long InsertedHashes,
      bool Skipped);

   /// <summary>
   /// Result of <see cref="HibpBloomBuilder.BuildAsync"/>.
   /// </summary>
   public readonly record struct HibpBloomBuildResult(
      string OutputPath,
      bool Skipped,
      ulong InsertedCount,
      DateTime BuiltUtc);
}
