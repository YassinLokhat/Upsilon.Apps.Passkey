using Upsilon.Apps.PassKey.Core.Abstraction.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Implementation.Models
{
   internal class Log : ILog
   {
      public DateTime DateTime { get; set; }

      public string Message { get; set; } = string.Empty;

      public bool NeedsReview { get; set; }
   }
}
