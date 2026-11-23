using FluentAssertions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;

namespace Upsilon.Apps.Passkey.UnitTests.Windows.Gui
{
   /// <summary>
   /// The selection transition leaves a part alone when it draws the same thing
   /// before and after; anything visibly different must count as changed.
   /// </summary>
   [STATestClass]
   public sealed class VectorSnapshotTests
   {
      [TestMethod]
      public void Snapshot_LeavesTheElementsOwnObjectsEditable()
      {
         // A copied shape must still measure and change afterwards (it updates its
         // own pen), and an element's own transform must stay animatable.
         TranslateTransform move = new(0, 4);
         Rectangle dot = new() { Width = 6, Height = 6, Stroke = Brushes.White, StrokeThickness = 1, RenderTransform = move };
         Border root = new() { Child = dot };
         root.Measure(new Size(50, 50));
         root.Arrange(new Rect(0, 0, 50, 50));

         _ = VectorSnapshot.Of(root);

         _ = move.IsFrozen.Should().BeFalse();
         Action reuse = () =>
         {
            move.Y = 8;
            dot.StrokeThickness = 2;
            dot.Width = 8;
            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(0, 0, 50, 50));
            _ = VectorSnapshot.Of(root);
         };
         _ = reuse.Should().NotThrow();
      }

      [TestMethod]
      public void SameText_Matches()
      {
         _ = VectorSnapshot.Of(_text("Account 0")).Matches(VectorSnapshot.Of(_text("Account 0"))).Should().BeTrue();
      }

      [TestMethod]
      public void DifferentText_DoesNotMatch()
      {
         _ = VectorSnapshot.Of(_text("Account 0")).Matches(VectorSnapshot.Of(_text("Account 1"))).Should().BeFalse();
      }

      [TestMethod]
      public void DifferentColour_DoesNotMatch()
      {
         _ = VectorSnapshot.Of(_text("Account 0", Brushes.White)).Matches(VectorSnapshot.Of(_text("Account 0", Brushes.Red))).Should().BeFalse();
      }

      [TestMethod]
      public void OnlyAnInvisibleBackgroundResized_Matches()
      {
         // A field that got narrower (a scrollbar appeared) with the same text.
         FrameworkElement wide = _framed("Account 0", 300);
         FrameworkElement narrow = _framed("Account 0", 280);

         _ = VectorSnapshot.Of(wide).Matches(VectorSnapshot.Of(narrow)).Should().BeTrue();
      }

      [TestMethod]
      public void VisibleBackgroundResized_DoesNotMatch()
      {
         FrameworkElement wide = _framed("Account 0", 300, Brushes.DimGray);
         FrameworkElement narrow = _framed("Account 0", 280, Brushes.DimGray);

         _ = VectorSnapshot.Of(wide).Matches(VectorSnapshot.Of(narrow)).Should().BeFalse();
      }

      [TestMethod]
      public void CollapsedChild_IsNotPartOfTheDrawing()
      {
         StackPanel shown = new();
         _ = shown.Children.Add(new TextBlock { Text = "Account 0" });
         StackPanel hidden = new();
         _ = hidden.Children.Add(new TextBlock { Text = "Account 0" });
         _ = hidden.Children.Add(new TextBlock { Text = "secret", Visibility = Visibility.Collapsed });

         _ = VectorSnapshot.Of(_laidOut(shown)).Matches(VectorSnapshot.Of(_laidOut(hidden))).Should().BeTrue();
      }

      private static FrameworkElement _text(string text, Brush? foreground = null)
         => _laidOut(new TextBlock { Text = text, FontSize = 13, Foreground = foreground ?? Brushes.White });

      private static FrameworkElement _framed(string text, double width, Brush? background = null)
         => _laidOut(new Border
         {
            Width = width,
            Background = background ?? Brushes.Transparent,
            Child = new TextBlock { Text = text, FontSize = 13, Foreground = Brushes.White },
         });

      private static FrameworkElement _laidOut(FrameworkElement element)
      {
         element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
         element.Arrange(new Rect(element.DesiredSize));
         element.UpdateLayout();
         return element;
      }
   }
}
