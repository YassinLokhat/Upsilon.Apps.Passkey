namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Foreground / background signals for session lock and idle timers.</summary>
   internal interface IAppLifecycle
   {
      event EventHandler? EnteredBackground;

      event EventHandler? Resumed;
   }
}
