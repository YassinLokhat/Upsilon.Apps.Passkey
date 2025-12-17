using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent an event log.
   /// </summary>
   public interface ILog
   {
      /// <summary>
      /// The date and time the event occured.
      /// </summary>
      DateTime DateTime { get; }

      /// <summary>
      /// The source of the event.
      /// </summary>
      string Source { get; }

      /// <summary>
      /// The target of the event.
      /// </summary>
      string Target { get; }

      /// <summary>
      /// The raw event data.
      /// </summary>
      string Data { get; }

      /// <summary>
      /// The event type.
      /// </summary>
      LogEventType EventType { get; }

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
