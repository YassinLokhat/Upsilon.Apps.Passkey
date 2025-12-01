namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
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
      /// The event message.
      /// </summary>
      string Message { get; }

      /// <summary>
      /// Indicate if the current log needs review.
      /// </summary>
      bool NeedsReview { get; set; }
   }
}
