using System.Windows.Media;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Runs an action once the next frame has been drawn: after the render pass
   /// that lays out what was just created (however long that layout takes), at
   /// the start of the pass that follows it. Animations of something new start
   /// there, so their first visible frame is their first frame. A short backstop
   /// runs the action anyway when no frame comes (a minimized window).
   /// </summary>
   internal static class FrameGate
   {
      private static readonly TimeSpan _backstop = TimeSpan.FromMilliseconds(150);

      public static void AfterNextDrawnFrame(Dispatcher dispatcher, Action action)
      {
         ArgumentNullException.ThrowIfNull(dispatcher);
         ArgumentNullException.ThrowIfNull(action);

         int frames = 0;
         TimeSpan lastFrame = TimeSpan.MinValue;
         bool done = false;
         DispatcherTimer backstop = new(DispatcherPriority.Background, dispatcher) { Interval = _backstop };
         EventHandler? onFrame = null;

         void Run()
         {
            if (done)
            {
               return;
            }

            done = true;
            backstop.Stop();
            CompositionTarget.Rendering -= onFrame;
            action();
         }

         onFrame = (_, e) =>
         {
            // Rendering can be raised more than once for one frame.
            TimeSpan time = e is RenderingEventArgs rendering ? rendering.RenderingTime : TimeSpan.Zero;
            if (time == lastFrame)
            {
               return;
            }

            lastFrame = time;
            if (++frames >= 2)
            {
               Run();
            }
         };

         backstop.Tick += (_, _) => Run();
         CompositionTarget.Rendering += onFrame;
         backstop.Start();
      }
   }
}
