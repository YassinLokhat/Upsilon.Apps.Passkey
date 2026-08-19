using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IActivity
   {
      DateTime DateTime { get; }

      string ItemId { get; }

      ActivityEventType EventType { get; }

      bool NeedsReview { get; set; }

      string Message { get; }
   }
}
