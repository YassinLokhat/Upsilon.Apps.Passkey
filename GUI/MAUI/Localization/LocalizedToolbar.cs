using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   internal static class LocalizedToolbar
   {
      /// <summary>Adds a toolbar item whose text follows the UI language, like <c>{loc:Loc}</c>.</summary>
      public static void AddLocalized(this IList<ToolbarItem> items, string key, ICommand command, ToolbarItemOrder order = ToolbarItemOrder.Primary)
      {
         ArgumentNullException.ThrowIfNull(items);
         ToolbarItem item = new() { Command = command, Order = order };
         item.SetBinding(MenuItem.TextProperty, LocExtension.For(key));
         items.Add(item);
      }
   }
}
