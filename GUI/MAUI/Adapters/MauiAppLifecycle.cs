using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiAppLifecycle : IAppLifecycle
   {
      public event EventHandler? EnteredBackground;
      public event EventHandler? Resumed;

      public MauiAppLifecycle()
      {
         // Wire once the app is running; hosts call Notify* from App.xaml.cs.
      }

      public void NotifyEnteredBackground() => EnteredBackground?.Invoke(this, EventArgs.Empty);

      public void NotifyResumed() => Resumed?.Invoke(this, EventArgs.Empty);
   }
}
