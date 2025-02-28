namespace Upsilon.Apps.PassKey.Core.Events
{
   /// <summary>
   /// Represent a loggout event argument.
   /// <param name="loginTimeoutReached">Indicate if the logout event is due to a reached timeout.</param>
   /// </summary>
   public sealed class LogoutEventArgs(bool loginTimeoutReached) : EventArgs
   {
      /// <summary>
      /// Indicate if the logout event is due to a reached timeout.
      /// </summary>
      public bool LoginTimeoutReached { get; private set; } = loginTimeoutReached;
   }
}
