using PresentationLauncher = Upsilon.Apps.Passkey.GUI.Presentation.Ports.ILauncher;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiLauncher : PresentationLauncher
   {
      public Task OpenUriAsync(Uri uri, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(uri);
         return Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
      }
   }
}
