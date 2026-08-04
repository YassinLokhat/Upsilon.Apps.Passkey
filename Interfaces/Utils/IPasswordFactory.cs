namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Represent a Password factory engine.
   /// </summary>
   public interface IPasswordFactory
   {
      /// <summary>
      /// The letters used by the factory.
      /// </summary>
      string Alphabetic { get; }

      /// <summary>
      /// The digits used by the factory.
      /// </summary>
      string Numeric { get; }

      /// <summary>
      /// The special characters used by the factory.
      /// </summary>
      string SpecialChars { get; }

      /// <summary>
      /// Generate a random password.
      /// </summary>
      /// <param name="length">The length of the password.</param>
      /// <param name="alphabet">The alphabet used.</param>
      /// <param name="checkIfLeaked">Ensure that the generated password has been already leaked.</param>
      /// <returns>The random geenrated password.</returns>
      string GeneratePassword(int length,
         string alphabet,
         bool checkIfLeaked = true);

      /// <summary>
      /// Generate a random password without blocking the calling thread.
      /// When <paramref name="checkIfLeaked"/> is set, the leak check is awaited
      /// instead of blocking, so a UI thread stays responsive while the remote
      /// service answers.
      /// </summary>
      /// <param name="length">The length of the password.</param>
      /// <param name="alphabet">The alphabet used.</param>
      /// <param name="checkIfLeaked">Ensure that the generated password has been already leaked.</param>
      /// <param name="cancellationToken">Cancels the pending leak checks.</param>
      /// <returns>The random geenrated password.</returns>
      Task<string> GeneratePasswordAsync(int length,
         string alphabet,
         bool checkIfLeaked = true,
         CancellationToken cancellationToken = default);

      /// <summary>
      /// Check if the password has been leaked.
      /// </summary>
      /// <param name="password">The password to check.</param>
      /// <returns>Returns true if the password has been leaked.</returns>
      bool PasswordLeaked(string password);

      /// <summary>
      /// Check if the password has been leaked, awaiting the remote service
      /// instead of blocking the calling thread.
      /// </summary>
      /// <param name="password">The password to check.</param>
      /// <param name="cancellationToken">Cancels the pending request.</param>
      /// <returns>Returns true if the password has been leaked.</returns>
      Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default);
   }
}
