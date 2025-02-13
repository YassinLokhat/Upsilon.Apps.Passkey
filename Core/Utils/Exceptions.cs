namespace Upsilon.Apps.PassKey.Core.Utils
{
   public sealed class CheckSignFailedException : Exception { }

   public sealed class CorruptedSourceException : Exception { }

   public sealed class WrongPasswordException(int passwordLevel) : Exception()
   {
      public int PasswordLevel { get; private set; } = passwordLevel;
   }
}
