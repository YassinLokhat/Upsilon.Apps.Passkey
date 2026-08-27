namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Optimal Bloom-filter sizing for a target capacity and false-positive rate.
   /// </summary>
   public static class BloomSizing
   {
      /// <summary>
      /// Default expected HIBP SHA-1 unique-hash count used when building a full filter.
      /// </summary>
      public const ulong DefaultCapacity = 2_100_000_000UL;

      /// <summary>
      /// Default target false-positive rate (~1 %).
      /// </summary>
      public const double DefaultFalsePositiveRate = 0.01;

      /// <summary>
      /// Computes bit-array length <c>m</c> and hash-function count <c>k</c>.
      /// </summary>
      public static (ulong BitCount, int HashFunctions) For(ulong capacity, double falsePositiveRate)
      {
         ArgumentOutOfRangeException.ThrowIfZero(capacity);

         if (falsePositiveRate is <= 0 or >= 1)
         {
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));
         }

         double n = capacity;
         double p = falsePositiveRate;
         double m = Math.Ceiling(-n * Math.Log(p) / (Math.Log(2) * Math.Log(2)));
         int k = Math.Max(1, (int)Math.Round(m / n * Math.Log(2)));

         return ((ulong)m, k);
      }
   }
}
