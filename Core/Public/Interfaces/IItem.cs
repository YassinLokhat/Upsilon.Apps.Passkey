namespace Upsilon.Apps.Passkey.Core.Public.Interfaces
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
   }
}
