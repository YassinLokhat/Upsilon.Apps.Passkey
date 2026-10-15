using System.Diagnostics;
using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   internal sealed class WpfLauncher : ILauncher
   {
      public Task OpenUriAsync(Uri uri, CancellationToken cancellationToken = default)
      {
         ArgumentNullException.ThrowIfNull(uri);
         using Process? process = Process.Start(new ProcessStartInfo
         {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true,
         });
         return Task.CompletedTask;
      }
   }
}
