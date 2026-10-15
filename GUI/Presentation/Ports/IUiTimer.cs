namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>UI-thread timer (replaces <c>DispatcherTimer</c>).</summary>
   internal interface IUiTimer : IDisposable
   {
      TimeSpan Interval { get; set; }

      event EventHandler? Tick;

      void Start();

      void StopTimer();
   }

   internal interface IUiTimerFactory
   {
      IUiTimer Create();
   }
}
