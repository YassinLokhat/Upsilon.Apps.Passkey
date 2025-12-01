namespace Upsilon.Apps.Passkey.Core.Public.Events
{
   /// <summary>
   /// Represent a loggout event argument.
   /// <param name="loginTimeoutReached">Indicate if the logout event is due to a reached timeout.</param>
   /// </summary>
   public class LogoutEventArgs(bool loginTimeoutReached) : EventArgs
   {
      public bool LoginTimeoutReached { get; private set; } = loginTimeoutReached;
   }
}
