namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   public sealed class CorruptedSourceException : Exception
   {
      public CorruptedSourceException() : base() { }
      public CorruptedSourceException(string message) : base(message) { }
      public CorruptedSourceException(string message, Exception innerException) : base(message, innerException) { }
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
