using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Raised when the warning scan finishes (may run on a worker thread).
   /// </summary>
   /// <param name="warning">Warnings that match the user's notify settings.</param>
   public class WarningsUpdatedEventArgs(IEnumerable<IWarning> warning) : EventArgs
   {
      public IEnumerable<IWarning> Warnings { get; private set; } = warning;
   }
}
