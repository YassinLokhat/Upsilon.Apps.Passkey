namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   public static class StaticMethods
   {
      public static bool HasChanged(this IItem item)
      {
         return item.Database.HasChanged(item.ItemId);
      }

      public static bool HasChanged(this IItem item, string fieldName)
      {
         return item.Database.HasChanged(item.ItemId, fieldName);
      }
   }
}
