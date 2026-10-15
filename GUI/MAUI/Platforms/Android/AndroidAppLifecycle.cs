using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Android
{
   /// <summary>Raised from <see cref="App"/>'s OnSleep / OnResume.</summary>
   internal sealed class AndroidAppLifecycle : IAppLifecycle
   {
      public event EventHandler? EnteredBackground;
      public event EventHandler? Resumed;

      public void NotifyEnteredBackground() => EnteredBackground?.Invoke(this, EventArgs.Empty);

      public void NotifyResumed() => Resumed?.Invoke(this, EventArgs.Empty);
   }
}
