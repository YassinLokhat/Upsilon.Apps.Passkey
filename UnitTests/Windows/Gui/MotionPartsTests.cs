using FluentAssertions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// The pieces behind the shared open/close motion and the pre-drawn glow:
   /// nothing here may depend on a window being on screen.
   /// </summary>
   [STATestClass]
   [DoNotParallelize]
   public sealed class MotionPartsTests
   {
      [TestMethod]
      public void GlowRings_FromAFill_FadeOutwards()
      {
         (double Offset, double Alpha)[] rings = [.. GlowHalo.Rings(GlowSource.Fill, 12, 0.28)];

         _ = rings.Should().OnlyContain(r => r.Offset > 0, "a filled shape covers its own inside");
         _ = rings.Select(r => r.Alpha).Should().BeInDescendingOrder();
         _ = rings[0].Alpha.Should().BeInRange(0.28 * 0.4, 0.28 * 0.5, "just outside the edge a blur keeps about half its strength");
         _ = rings[^1].Alpha.Should().BeLessThan(0.28 * 0.01, "it has faded out by the blur radius");
      }

      [TestMethod]
      public void GlowRings_FromALine_PeakOnTheLineAndFadeBothWays()
      {
         (double Offset, double Alpha)[] rings = [.. GlowHalo.Rings(GlowSource.Line, 12, 0.28)];

         (double Offset, double Alpha) peak = rings.MaxBy(r => r.Alpha);
         _ = peak.Offset.Should().Be(-0.5, "the 1 px line sits just inside the edge");
         _ = rings.Where(r => r.Offset > peak.Offset).Select(r => r.Alpha).Should().BeInDescendingOrder();
         _ = rings.Where(r => r.Offset < peak.Offset).Select(r => r.Alpha).Should().BeInAscendingOrder();
      }

      [TestMethod]
      public void FrameGate_RunsTheActionEvenWhenNoFrameComes()
      {
         bool ran = false;
         FrameGate.AfterNextDrawnFrame(Dispatcher.CurrentDispatcher, () => ran = true);

         _pump(TimeSpan.FromMilliseconds(400));

         _ = ran.Should().BeTrue();
      }

      [TestMethod]
      public void HidingClearsADrawingAtOnce_SoTheCopyIsTakenFirst()
      {
         // Why AnimateVisibility owns the hide: once collapsed, nothing is left to copy.
         (Border badge, _, HwndSource source) = _host();
         using (source)
         {
            Rect shown = VectorSnapshot.Of(badge).Drawing.Bounds;

            badge.Visibility = Visibility.Collapsed;

            _ = shown.Should().Be(new Rect(0, 0, 40, 16));
            _ = VectorSnapshot.Of(badge).Drawing.Bounds.Should().Be(Rect.Empty);
         }
      }

      [TestMethod]
      public void AnimateVisibility_HidesAtOnce_AndLeavesAFadingCopyThatRemovesItself()
      {
         (Border badge, StackPanel row, HwndSource source) = _host();
         using (source)
         {
            Vector place = VisualTreeHelper.GetOffset(badge);
            Motion.SetAnimateVisibility(badge, Visibility.Collapsed);

            _ = badge.Visibility.Should().Be(Visibility.Collapsed, "the layout changes at once");
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(row)!;
            if (!Motion.Enabled)
            {
               _ = layer.GetAdorners(row).Should().BeNull("with Windows animations off nothing fades");
               return;
            }

            Adorner copy = layer.GetAdorners(row).Should().ContainSingle("the copy fades where the badge was").Subject;
            copy.UpdateLayout();
            _ = VectorSnapshot.Of(copy).Drawing.Bounds.Should().Be(new Rect(place.X, place.Y, 40, 16), "the copy draws what the badge drew, where it was");

            _pump(TimeSpan.FromMilliseconds(800));

            _ = layer.GetAdorners(row).Should().BeNull("the copy is removed once faded");

            Motion.SetAnimateVisibility(badge, Visibility.Visible);
            _ = badge.Visibility.Should().Be(Visibility.Visible);
         }
      }

      // A badge laid out in a hidden input source (nothing is shown on screen).
      private static (Border Badge, StackPanel Row, HwndSource Source) _host()
      {
         Border badge = new() { Width = 40, Height = 16, Background = Brushes.Teal };
         StackPanel row = new() { Orientation = Orientation.Horizontal, Children = { badge, new TextBlock { Text = "badge" } } };
         AdornerDecorator decorator = new() { Child = row };
         HwndSource source = new(new HwndSourceParameters(nameof(MotionPartsTests)) { Width = 200, Height = 60, WindowStyle = 0 })
         {
            RootVisual = decorator,
         };
         decorator.Measure(new Size(200, 60));
         decorator.Arrange(new Rect(0, 0, 200, 60));
         decorator.UpdateLayout();
         _pump(TimeSpan.FromMilliseconds(100));
         _ = badge.IsLoaded.Should().BeTrue();
         return (badge, row, source);
      }

      private static void _pump(TimeSpan duration)
      {
         DispatcherFrame frame = new();
         DispatcherTimer stop = new(DispatcherPriority.Background) { Interval = duration };
         stop.Tick += (_, _) =>
         {
            stop.Stop();
            frame.Continue = false;
         };
         stop.Start();
         Dispatcher.PushFrame(frame);
      }
   }
}
