using Upsilon.Apps.Passkey.GUI.WPF.Services;

namespace Upsilon.Apps.Passkey.UnitTests.Gui.Fakes
{
   internal sealed class FakeNavigationService : INavigationService
   {
      public List<string> RequestedItems { get; } = [];

      public event EventHandler<string>? ItemRequested;

      public void RequestItem(string itemId)
      {
         RequestedItems.Add(itemId);
         ItemRequested?.Invoke(this, itemId);
      }
   }
}
