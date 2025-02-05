using Upsilon.Apps.PassKey.Core.Public.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Internal.Models
{
   internal class Log : ILog
   {
      public DateTime DateTime { get; set; }

      public string Message { get; set; } = string.Empty;

      public bool NeedsReview { get; set; }
   }
}
