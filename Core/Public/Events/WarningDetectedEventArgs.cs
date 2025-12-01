using Upsilon.Apps.Passkey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Public.Events
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
