using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Fakes
{
   /// <summary>
   /// Deterministic leak-check double so warning-scan tests never hit the network.
   /// </summary>
   internal sealed class FakePasswordFactory : IPasswordFactory
   {
      private readonly HashSet<string> _leaked = new(StringComparer.Ordinal);

      public string Alphabetic => "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      public string Numeric => "0123456789";
      public string SpecialChars => "!@#";

      public void MarkLeaked(string password) => _ = _leaked.Add(password);

      public string GeneratePassword(int length, string alphabet, bool checkIfLeaked = true)
         => alphabet.Length == 0 || length <= 0 ? string.Empty : new string(alphabet[0], length);

      public Task<string> GeneratePasswordAsync(int length, string alphabet, bool checkIfLeaked = true, CancellationToken cancellationToken = default)
         => Task.FromResult(GeneratePassword(length, alphabet, checkIfLeaked));

      public bool PasswordLeaked(string password) => _leaked.Contains(password);

      public Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default)
         => Task.FromResult(PasswordLeaked(password));
   }
}
