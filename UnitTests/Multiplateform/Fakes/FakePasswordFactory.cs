using Upsilon.Apps.Passkey.Interfaces.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Multiplateform.Fakes
{
   /// <summary>
   /// Deterministic leak-check double so alert-scan tests never hit the network.
   /// Optional <see cref="LeakGate"/> / <see cref="ThrowOnLeakCheck"/> control
   /// the slow phase for two-phase scan regression tests.
   /// </summary>
   internal sealed class FakePasswordFactory : IPasswordFactory
   {
      private readonly HashSet<string> _leaked = new(StringComparer.Ordinal);

      public string UpperAlphabetic => "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      public string LowerAlphabetic => "abcdefghijklmnopqrstuvwxyz";
      public string Numeric => "0123456789";
      public string SpecialChars => "!@#";

      public bool HasLocalFilter { get; set; }

      /// <summary>
      /// When set, every <see cref="PasswordLeakedAsync"/> awaits this task
      /// before returning (lets tests observe the local publish phase first).
      /// </summary>
      public TaskCompletionSource<bool>? LeakGate { get; set; }

      /// <summary>
      /// When true, <see cref="PasswordLeakedAsync"/> throws
      /// <see cref="InvalidOperationException"/> (non-<c>NullValueException</c>).
      /// </summary>
      public bool ThrowOnLeakCheck { get; set; }

      public void MarkLeaked(string password) => _ = _leaked.Add(password);

      public string GeneratePassword(int length, string alphabet, bool checkIfLeaked = true)
         => alphabet.Length == 0 || length <= 0 ? string.Empty : new string(alphabet[0], length);

      public Task<string> GeneratePasswordAsync(int length, string alphabet, bool checkIfLeaked = true, CancellationToken cancellationToken = default)
         => Task.FromResult(GeneratePassword(length, alphabet, checkIfLeaked));

      public bool PasswordLeaked(string password) => _leaked.Contains(password);

      public async Task<bool> PasswordLeakedAsync(string password, CancellationToken cancellationToken = default)
      {
         if (ThrowOnLeakCheck)
         {
            throw new InvalidOperationException("Simulated leak-check failure.");
         }

         if (LeakGate is not null)
         {
            _ = await LeakGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
         }

         return PasswordLeaked(password);
      }
   }
}
