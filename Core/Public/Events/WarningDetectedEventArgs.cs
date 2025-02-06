using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Public.Events
{
   /// <summary>
   /// Represent a warning detected event argument.
   /// </summary>
   /// <param name="warning">The warnings detected.</param>
   public class WarningDetectedEventArgs(IWarning[] warning)
   {
      /// <summary>
      /// The warnings detected.
      /// </summary>
      public IWarning[] Warnings { get; private set; } = warning;
   }
}
