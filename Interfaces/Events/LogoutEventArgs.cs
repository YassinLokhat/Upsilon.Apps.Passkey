using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Arguments for <see cref="IDatabase.DatabaseClosed"/>.
   /// </summary>
   /// <param name="loginTimeoutReached">
   /// <see langword="true"/> when the session ended because of inactivity,
   /// <see langword="false"/> when the user (or <see cref="IDisposable.Dispose"/>) closed it.
   /// </param>
   public class LogoutEventArgs(bool loginTimeoutReached) : EventArgs
   {
      /// <summary>
      /// <see langword="true"/> when auto-logout fired; otherwise a deliberate close.
      /// </summary>
      public bool LoginTimeoutReached { get; private set; } = loginTimeoutReached;
   }
}
