namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Named Bloom false-positive-rate presets for the offline HIBP filter.
   /// Codes are persisted in the WPF host <c>config.json</c>.
   /// </summary>
   public static class LeakFilterQuality
   {
      public const string BalancedCode = "Balanced";
      public const string StrictCode = "Strict";
      public const string ParanoidCode = "Paranoid";

      /// <summary>~1 % target FPR (default; ~2.4 GiB at <see cref="BloomSizing.DefaultCapacity"/>).</summary>
      public const double BalancedRate = 0.01;

      /// <summary>~0.1 % target FPR (~3.5 GiB).</summary>
      public const double StrictRate = 0.001;

      /// <summary>~0.01 % target FPR (~4.7 GiB).</summary>
      public const double ParanoidRate = 0.0001;

      /// <summary>
      /// Maps a persisted quality code to a false-positive rate. Unknown or empty
      /// values resolve to <see cref="BalancedCode"/>.
      /// </summary>
      public static double RateFromCode(string? code)
      {
         return string.Equals(code, StrictCode, StringComparison.OrdinalIgnoreCase)
            ? StrictRate
            : string.Equals(code, ParanoidCode, StringComparison.OrdinalIgnoreCase) ? ParanoidRate : BalancedRate;
      }

      /// <summary>
      /// Maps a false-positive rate to the nearest known quality code. Values that
      /// are not an exact preset fall back to <see cref="BalancedCode"/>.
      /// </summary>
      public static string CodeFromRate(double falsePositiveRate)
      {
         return falsePositiveRate == StrictRate
            ? StrictCode
            : falsePositiveRate == ParanoidRate ? ParanoidCode : falsePositiveRate == BalancedRate ? BalancedCode : BalancedCode;
      }

      /// <summary>
      /// Known codes in display order (Balanced, Strict, Paranoid).
      /// </summary>
      public static IReadOnlyList<string> Codes { get; } =
      [
         BalancedCode,
         StrictCode,
         ParanoidCode,
      ];

      /// <summary>
      /// Approximate on-disk size in GiB for <paramref name="falsePositiveRate"/>
      /// at <see cref="BloomSizing.DefaultCapacity"/> (header + bit array).
      /// </summary>
      public static double ApproximateSizeGiB(double falsePositiveRate)
      {
         (ulong bitCount, _) = BloomSizing.For(BloomSizing.DefaultCapacity, falsePositiveRate);
         long bytes = HibpBloomFile.HeaderSize + (long)((bitCount + 7UL) / 8UL);
         return bytes / (1024d * 1024d * 1024d);
      }

      /// <summary>
      /// Whether <paramref name="capacity"/> / <paramref name="falsePositiveRate"/>
      /// produce the same bit-array parameters as an existing filter header.
      /// </summary>
      public static bool SizingMatches(
         ulong capacity,
         double falsePositiveRate,
         ulong fileCapacity,
         ulong fileBitCount,
         int fileHashFunctions)
      {
         (ulong bitCount, int hashFunctions) = BloomSizing.For(capacity, falsePositiveRate);
         return fileCapacity == capacity
            && fileBitCount == bitCount
            && fileHashFunctions == hashFunctions;
      }

      /// <summary>
      /// Whether the <c>.pkbf</c> at <paramref name="path"/> is sized for
      /// <paramref name="capacity"/> and <paramref name="falsePositiveRate"/>.
      /// </summary>
      public static bool FileSizingMatches(string path, ulong capacity, double falsePositiveRate)
      {
         return HibpBloomFile.TryReadSizing(path, out ulong fileCapacity, out ulong fileBitCount, out int fileHashFunctions) && SizingMatches(capacity, falsePositiveRate, fileCapacity, fileBitCount, fileHashFunctions);
      }
   }
}
