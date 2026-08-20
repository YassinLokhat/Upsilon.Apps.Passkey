using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Thrown when a vault payload is not a Passkey onion (wrong file, truncated
   /// archive, or tampered ciphertext). Distinct from
   /// <see cref="WrongPasswordException"/> so the GUI does not treat corruption
   /// as "try the next passkey".
   /// </summary>
   public sealed class CorruptedSourceException : Exception
   {
      public CorruptedSourceException() : base() { }
      public CorruptedSourceException(string message) : base(message) { }
      public CorruptedSourceException(string message, Exception innerException) : base(message, innerException) { }
   }

   /// <summary>
   /// Thrown when key-derivation parameters (e.g. from a database header) fall
   /// below the floor this crypto center accepts, so a weakened or malformed
   /// header is never used to stretch passkeys.
   /// </summary>
   public sealed class InsufficientKdfParametersException : Exception
   {
      public InsufficientKdfParametersException() : base() { }
      public InsufficientKdfParametersException(string message) : base(message) { }
      public InsufficientKdfParametersException(string message, Exception innerException) : base(message, innerException) { }
   }

   /// <summary>
   /// Thrown during progressive login when the supplied passkeys decrypted their
   /// onion layers successfully but the result is not yet a valid compressed
   /// payload — typically because more passkeys are still required. Callers
   /// treat this like a soft login miss (<see langword="null"/>), not corruption.
   /// </summary>
   public sealed class IncompleteOnionException : Exception
   {
      public IncompleteOnionException() : base() { }
      public IncompleteOnionException(string message) : base(message) { }
      public IncompleteOnionException(string message, Exception innerException) : base(message, innerException) { }
   }

   /// <summary>
   /// Thrown when an onion layer fails authentication: the passkey at that depth
   /// is wrong (or a previous mistype poisoned the stack).
   /// <see cref="IDatabase.Login"/> catches this and returns <see langword="null"/>;
   /// there is no rollback of the stacked passkey.
   /// </summary>
   public sealed class WrongPasswordException : Exception
   {
      /// <summary>
      /// 1-based index of the onion layer that failed (username layer is 1).
      /// Useful in activity logs; not a remaining-attempts counter.
      /// </summary>
      public int PasswordLevel { get; private set; }

      public WrongPasswordException(int passwordLevel) : base()
      {
         PasswordLevel = passwordLevel;
      }

      public WrongPasswordException(int passwordLevel, string message) : base(message)
      {
         PasswordLevel = passwordLevel;
      }

      public WrongPasswordException(int passwordLevel, string message, Exception innerException) : base(message, innerException)
      {
         PasswordLevel = passwordLevel;
      }

      public WrongPasswordException() { }

      public WrongPasswordException(string message) : base(message) { }

      public WrongPasswordException(string message, Exception innerException) : base(message, innerException) { }
   }

   /// <summary>
   /// Thrown when a required object is missing at runtime (typically
   /// <c>IDatabase.User</c> before login, or an unset parent on a model).
   /// Not a substitute for <see cref="ArgumentNullException"/> on public parameters.
   /// </summary>
   public sealed class NullValueException : Exception
   {
      /// <summary>
      /// The name of the missing value (often a parameter or property name).
      /// </summary>
      public string Name { get; private set; } = string.Empty;

      public NullValueException() { }

      public NullValueException(string message) : base($"Value named '{message}'is null.")
      {
         Name = message;
      }

      public NullValueException(string message, Exception innerException) : base($"Value named '{message}'is null.", innerException)
      {
         Name = message;
      }
   }
}
