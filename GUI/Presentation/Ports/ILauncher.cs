namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Opens http(s) URLs via the OS (replaces <c>Process.Start</c>).</summary>
   internal interface ILauncher
   {
      Task OpenUriAsync(Uri uri, CancellationToken cancellationToken = default);
   }
}
