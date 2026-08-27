namespace Upsilon.Apps.Passkey.Utils.LeakFilter
{
   /// <summary>
   /// Local, offline membership probe for SHA-1 password hashes (HIBP corpus).
   /// Used only after remote leak providers fail. A miss is definitive (no false
   /// negatives); a hit may be a false positive and is treated conservatively.
   /// </summary>
   public interface ILocalLeakFilter : IDisposable
   {
      /// <summary>
      /// Returns <see langword="true"/> when the filter believes the SHA-1 hash
      /// might be in the corpus (possible false positive).
      /// </summary>
      bool MightContain(ReadOnlySpan<byte> sha1);

      /// <summary>
      /// Absolute path of the backing <c>.pkbf</c> file, when applicable.
      /// </summary>
      string? Path { get; }

      /// <summary>
      /// UTC time the filter was built, from the file header.
      /// </summary>
      DateTime BuiltUtc { get; }

      /// <summary>
      /// Number of hashes recorded as inserted when the file was written.
      /// </summary>
      ulong InsertedCount { get; }
   }
}
