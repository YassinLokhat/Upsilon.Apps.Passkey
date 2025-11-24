using Upsilon.Apps.Passkey.Core.Public.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Internal.Models
{
   internal class Log : ILog
   {
      public DateTime DateTime { get; set; }

      public string Message { get; set; } = string.Empty;

      public bool NeedsReview { get; set; }
   }
}
