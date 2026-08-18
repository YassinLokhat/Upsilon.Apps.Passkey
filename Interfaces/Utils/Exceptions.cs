namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
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

   public sealed class WrongPasswordException : Exception
   {
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

   public sealed class NullValueException : Exception
   {
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
