using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent an event activity.
   /// </summary>
   public interface IActivity
   {
      /// <summary>
      /// The date and time the event occured.
      /// </summary>
      DateTime DateTime { get; }

      /// <summary>
      /// The item id rizing the event.
      /// </summary>
      string ItemId { get; }

      /// <summary>
      /// The event type.
      /// </summary>
      ActivityEventType EventType { get; }

      /// <summary>
      /// Indicate if the current log needs review.
      /// </summary>
      bool NeedsReview { get; set; }

      /// <summary>
      /// The event message.
      /// </summary>
      string Message { get; }
   }
}
