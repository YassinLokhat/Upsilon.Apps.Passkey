using Upsilon.Apps.PassKey.Core.Abstraction.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Abstraction.Events
{
   /// <summary>
   /// Represent a warning detected event argument.
   /// </summary>
   /// <remarks>
   /// Creates a new event args.
   /// </remarks>
   /// <param name="warning">The warnings detected.</param>
   public class WarningDetectedEventArgs(IWarning[] warning)
   {
      /// <summary>
      /// The warnings detected.
      /// </summary>
      public IWarning[] Warnings { get; private set; } = warning;
   }
}
