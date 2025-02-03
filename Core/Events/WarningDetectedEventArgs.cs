using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upsilon.Apps.PassKey.Core.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Events
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
