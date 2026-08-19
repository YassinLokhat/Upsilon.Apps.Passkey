using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Convenience extensions over <see cref="IItem"/> that forward to the owning database.
   /// </summary>
   public static class StaticMethods
   {
      /// <summary>
      /// Whether <paramref name="fieldName"/> on <paramref name="item"/> has an
      /// unsaved autosave change. Reads the item's <see cref="IItem.Database"/>.
      /// </summary>
      public static bool HasChanged(this IItem item, string fieldName)
      {
         ArgumentNullException.ThrowIfNull(item);

         return item.Database.HasChanged(item.ItemId, fieldName);
      }
   }
}
