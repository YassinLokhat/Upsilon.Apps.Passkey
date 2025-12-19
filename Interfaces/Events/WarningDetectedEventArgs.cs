using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Events
{
   /// <summary>
   /// Represent a warning detected event argument.
   /// </summary>
   /// <param name="warning">The warnings detected.</param>
   public class WarningDetectedEventArgs(IWarning[] warning) : EventArgs
   {
      /// <summary>
      /// The warnings detected.
      /// </summary>
      public IWarning[] Warnings { get; private set; } = warning;
   }
}
