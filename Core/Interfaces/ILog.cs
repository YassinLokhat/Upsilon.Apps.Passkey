namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   /// <summary>
   /// Represent an event log.
   /// </summary>
   public interface ILog
   {
      /// <summary>
      /// The date and time the event occured.
      /// </summary>
      public DateTime DateTime { get; }

      /// <summary>
      /// The identifiant of the item.
      /// </summary>
      public string ItemId { get; }

      /// <summary>
      /// The event message.
      /// </summary>
      public string Message { get; }
   }
}
