namespace Upsilon.Apps.Passkey.GUI.Presentation.Services
{
   internal sealed class ItemNavigationEventArgs(string itemId) : EventArgs
   {
      public string ItemId { get; } = itemId;
   }

   internal interface INavigationService
   {
      event EventHandler<ItemNavigationEventArgs>? ItemRequested;

      void RequestItem(string itemId);
   }

   internal sealed class NavigationService : INavigationService
   {
      public event EventHandler<ItemNavigationEventArgs>? ItemRequested;

      public void RequestItem(string itemId)
         => ItemRequested?.Invoke(this, new ItemNavigationEventArgs(itemId));
   }
}
