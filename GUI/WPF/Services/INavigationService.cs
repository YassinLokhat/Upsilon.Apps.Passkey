namespace Upsilon.Apps.Passkey.GUI.WPF.Services
{
   /// <summary>
   /// Decouples the navigation requests (such as "go to this item") from the
   /// view-models that emit them. Replaces the static <c>MainViewModel.GoToItem</c>
   /// delegate.
   /// </summary>
   internal interface INavigationService
   {
      /// <summary>Raised when a caller asks to navigate to a specific item.</summary>
      event EventHandler<string>? ItemRequested;

      /// <summary>Asks any subscriber to navigate to <paramref name="itemId"/>.</summary>
      void RequestItem(string itemId);
   }
}
