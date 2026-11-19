using FluentAssertions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// Something that appears must never be drawn at full strength first and then
   /// jump back to start its animation: every frame drawn before its motion runs
   /// shows its first values (invisible, offset).
   /// </summary>
   [STATestClass]
   [DoNotParallelize]
   public sealed class MotionFirstFrameTests
   {
      private HwndSource? _source;

      [TestCleanup]
      public void Cleanup() => _source?.Dispose();

      [TestMethod]
      public void Open_FirstFramesShowTheStart_NotTheEnd() => _check(Motion.Open);

      [TestMethod]
      public void DropIn_FirstFramesShowTheStart_NotTheEnd() => _check(Motion.DropIn);

      [TestMethod]
      public void FadeIn_FirstFramesShowTheStart_NotTheEnd() => _check(Motion.FadeIn);

      private void _check(Func<FrameworkElement, System.Windows.Media.Animation.Storyboard> appear)
      {
         if (!Motion.Enabled)
         {
            Assert.Inconclusive("Windows animations are off on this machine.");
         }

         Border shown = new() { Width = 100, Height = 60, Background = Brushes.Gray };
         // Hidden input/render source: nothing is shown on screen.
         _source = new HwndSource(new HwndSourceParameters(nameof(MotionFirstFrameTests)) { Width = 200, Height = 200, WindowStyle = 0 })
         {
            RootVisual = new Grid { Children = { shown } },
         };

         List<double> seen = [];
         EventHandler onFrame = (_, _) => seen.Add(shown.Opacity);
         CompositionTarget.Rendering += onFrame;
         _ = appear(shown);
         _ = shown.Opacity.Should().BeLessThan(0.05, "the start values apply the moment the motion starts, before any frame can be drawn");

         DispatcherFrame frame = new();
         DispatcherTimer stop = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(400) };
         stop.Tick += (_, _) =>
         {
            stop.Stop();
            frame.Continue = false;
         };
         stop.Start();
         Dispatcher.PushFrame(frame);
         CompositionTarget.Rendering -= onFrame;

         _ = seen.Should().NotBeEmpty();
         // Opacity only rises from the start: no frame is brighter than a later one.
         for (int i = 1; i < seen.Count; i++)
         {
            _ = seen[i].Should().BeGreaterThanOrEqualTo(seen[i - 1] - 0.001, $"frame {i} must not be dimmer than frame {i - 1} (a flash then a restart)");
         }

         _ = seen[0].Should().BeLessThan(0.05, "the first drawn frame shows the start");
         _ = shown.Opacity.Should().BeApproximately(1, 0.01);
      }
   }
}
