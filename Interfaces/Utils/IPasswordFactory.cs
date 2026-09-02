namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Password generation and opt-in leak checks.
   /// </summary>
   public interface IPasswordFactory
   {
      /// <summary>
      /// Whether an offline Bloom filter is currently attached for post-network fallback.
      /// </summary>
      bool HasLocalFilter { get; }

      string Alphabetic { get; }

      string Numeric { get; }

      string SpecialChars { get; }

      /// <summary>
      /// Generate a random password.
      /// </summary>
      /// <param name="checkIfLeaked">
      /// When <see langword="true"/>, reject candidates found in the leak corpora
      /// (up to a small retry budget; may return empty if every attempt is leaked
      /// or the check cannot complete).
      /// </param>
      /// <returns>The generated password, or empty when leak-checked generation gives up.</returns>
      string GeneratePassword(int length,
         string alphabet,
         bool checkIfLeaked = true);

      /// <summary>
      /// Same as <see cref="GeneratePassword"/> without blocking the caller; leak
      /// checks are awaited when enabled.
      /// </summary>
      Task<string> GeneratePasswordAsync(int length,
         string alphabet,
         bool checkIfLeaked = true,
         CancellationToken cancellationToken = default);

      bool PasswordLeaked(string password);

      /// <summary>
      /// Same as <see cref="PasswordLeaked"/> without blocking the caller.
      /// </summary>
      Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default);
   }
}
