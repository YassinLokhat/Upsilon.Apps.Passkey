namespace Upsilon.Apps.PassKey.Core.Public.Utils
{
   public sealed class CheckSignFailedException : Exception
   {
      public CheckSignFailedException() : base() { }
      public CheckSignFailedException(string message) : base(message) { }
      public CheckSignFailedException(string message, Exception innerException) : base(message, innerException) { }
   }

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
   }
}
