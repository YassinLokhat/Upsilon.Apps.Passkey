namespace Upsilon.Apps.PassKey.Core.Public.Events
{
   /// <summary>
   /// Represent a loggout event argument.
   /// <param name="loginTimeoutReached">Indicate if the logout event is due to a reached timeout.</param>
   /// </summary>
   public class LogoutEventArgs(bool loginTimeoutReached)
   {
      public bool LoginTimeoutExpired { get; private set; } = loginTimeoutReached;
   }
}
