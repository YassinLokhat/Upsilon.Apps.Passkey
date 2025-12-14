using Upsilon.Apps.Passkey.Interfaces;

namespace Upsilon.Apps.Passkey.Core.Models
{
   internal class Log : ILog
   {
      public DateTime DateTime { get; set; }

      public string Message { get; set; } = string.Empty;

      public bool NeedsReview { get; set; }
   }
}
