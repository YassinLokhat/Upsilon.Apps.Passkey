namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   /// <summary>
   /// Represent an item.
   /// </summary>
   public interface IItem
   {
      /// <summary>
      /// The Id of the item.
      /// </summary>
      string ItemId { get; }

      /// <summary>
      /// The database that contains the item.
      /// </summary>
      IDatabase Database { get; }

      /// <summary>
      /// Check if the current item has changed.
      /// </summary>
      /// <returns>True if the current item has changed, False else.</returns>
      bool HasChanged();
   }
}
