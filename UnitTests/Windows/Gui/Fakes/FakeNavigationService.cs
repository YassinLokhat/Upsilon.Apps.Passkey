using Upsilon.Apps.Passkey.GUI.Presentation.Services;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui.Fakes
{
   internal sealed class FakeNavigationService : INavigationService
   {
      public List<string> RequestedItems { get; } = [];

      public event EventHandler<ItemNavigationEventArgs>? ItemRequested;

      public void RequestItem(string itemId)
      {
         RequestedItems.Add(itemId);
         ItemRequested?.Invoke(this, new ItemNavigationEventArgs(itemId));
      }
   }
}
