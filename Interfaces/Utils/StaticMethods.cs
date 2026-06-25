using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   public static class StaticMethods
   {
      public static bool HasChanged(this IItem item, string fieldName) => item.Database.HasChanged(item.ItemId, fieldName);
   }
}
