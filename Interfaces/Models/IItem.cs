namespace Upsilon.Apps.Passkey.Interfaces.Models
{
   public interface IItem
   {
      string ItemId { get; }

      IDatabase Database { get; }

      /// <summary>
      /// Whether this item has any unsaved autosave change.
      /// </summary>
      bool HasChanged();
   }
}
