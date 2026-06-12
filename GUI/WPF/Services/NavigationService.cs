namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   internal sealed class NavigationService : INavigationService
   {
      public event EventHandler<string>? ItemRequested;

      public void RequestItem(string itemId)
      {
         ItemRequested?.Invoke(this, itemId);
      }
   }
}
