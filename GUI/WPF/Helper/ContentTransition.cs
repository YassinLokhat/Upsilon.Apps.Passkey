using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Crossfade + horizontal slide between the old and the new content of a layer.
   /// Only the parts marked with <see cref="Motion.TransitionTierProperty"/> move
   /// (field text, list items, ticks…); frames, captions and buttons stay still.
   /// <para>
   /// Usage: <see cref="Capture"/> just before the selection / DataContext changes
   /// (it snapshots each part into a bitmap), change the content, then
   /// <see cref="Play"/>. The snapshot sits in the adorner layer above the part and
   /// slides out while the real new content slides in from the other side, starting
   /// at 0.4 opacity, so the panel is never empty. Both are clipped to the part's
   /// bounds so nothing slides over a frame. Cosmetic only: nothing waits on it.
   /// </para>
   /// </summary>
   internal sealed class ContentTransition
   {
      private const double SLIDE_DISTANCE = 20;
      private const double NEW_CONTENT_START_OPACITY = 0.4;
      private static readonly TimeSpan _slideDuration = TimeSpan.FromMilliseconds(200);
      private static readonly TimeSpan _fadeDuration = TimeSpan.FromMilliseconds(120);
      private static readonly TimeSpan _tierStagger = TimeSpan.FromMilliseconds(30);
      private static readonly QuinticEase _easeOut = _frozenEase();

      // Cleanup of the transition currently running on a part, so a quick second
      // selection finishes the first one at once instead of stacking snapshots.
      private static readonly ConditionalWeakTable<FrameworkElement, Action> _running = new();

      private readonly FrameworkElement _root;
      private readonly List<TransitionPart> _parts;

      private ContentTransition(FrameworkElement root, List<TransitionPart> parts)
      {
         _root = root;
         _parts = parts;
      }

      /// <summary>The last transition started (lets the offscreen harness sample its frames).</summary>
      internal static Storyboard? LastStoryboard { get; private set; }

      /// <summary>
      /// Snapshots every marked part under <paramref name="root"/>. Returns null
      /// when there is nothing to animate (animations off, not shown yet).
      /// </summary>
      public static ContentTransition? Capture(FrameworkElement root)
      {
         ArgumentNullException.ThrowIfNull(root);

         if (!Motion.Enabled || !root.IsLoaded || !root.IsVisible)
         {
            return null;
         }

         List<(FrameworkElement Content, int Tier)> marked = [];
         _collect(root, marked);
         if (marked.Count == 0)
         {
            return null;
         }

         int firstTier = marked.Min(x => x.Tier);
         List<TransitionPart> parts = [];
         foreach ((FrameworkElement content, int tier) in marked)
         {
            Finish(content);

            if (!content.IsVisible
               || content.ActualWidth < 1
               || content.ActualHeight < 1
               || VisualTreeHelper.GetParent(content) is not UIElement host
               || AdornerLayer.GetAdornerLayer(host) is not AdornerLayer layer)
            {
               continue;
            }

            Rect bounds = content.TransformToAncestor(host).TransformBounds(new Rect(content.RenderSize));
            parts.Add(new TransitionPart(content, host, layer, bounds, _snapshot(content), (tier - firstTier) * _tierStagger));
         }

         return parts.Count == 0 ? null : new ContentTransition(root, parts);
      }

      /// <summary>
      /// Runs the transition. <paramref name="direction"/> &gt; 0: the new item is
      /// below the old one, content moves left (new comes in from the right);
      /// &lt; 0: the opposite; 0: a plain 120 ms crossfade (filtering, first load).
      /// </summary>
      public Storyboard Play(int direction)
      {
         // Measure the new content now so the clips use its real width.
         _root.UpdateLayout();

         double distance = SLIDE_DISTANCE * Math.Sign(direction);
         TimeSpan duration = direction == 0 ? _fadeDuration : _slideDuration;
         Storyboard storyboard = new();
         List<Action> cleanups = [];

         foreach (TransitionPart part in _parts)
         {
            SnapshotAdorner adorner = new(part.Host, part.Snapshot, part.Bounds);
            part.Layer.Add(adorner);

            FrameworkElement content = part.Content;
            Transform previousTransform = content.RenderTransform;
            Geometry? previousClip = content.Clip;
            TranslateTransform shift = new();
            TranslateTransform clipShift = new();
            double width = Math.Max(content.ActualWidth, part.Bounds.Width);
            content.RenderTransform = shift;

            // Clip horizontally to the part's own slot (the clip moves against the
            // shift, so it stays put on screen); vertically nothing changes.
            content.Clip = new RectangleGeometry(new Rect(0, -10000, width, 20000)) { Transform = clipShift };

            _keyFrames(storyboard, shift, TranslateTransform.XProperty, distance, 0, part.Delay, duration);
            _keyFrames(storyboard, clipShift, TranslateTransform.XProperty, -distance, 0, part.Delay, duration);
            _keyFrames(storyboard, content, UIElement.OpacityProperty, NEW_CONTENT_START_OPACITY, 1, part.Delay, duration);
            _keyFrames(storyboard, adorner.Shift, TranslateTransform.XProperty, 0, -distance, part.Delay, duration);
            _keyFrames(storyboard, adorner.Image, UIElement.OpacityProperty, 1, 0, part.Delay, duration);

            bool done = false;
            Action cleanup = () =>
            {
               if (done)
               {
                  return;
               }

               done = true;
               _ = _running.Remove(content);
               part.Layer.Remove(adorner);
               content.RenderTransform = previousTransform;
               content.Clip = previousClip;
            };

            cleanups.Add(cleanup);
         }

         // Ending early (a new selection) ends every part of this transition, so
         // no snapshot is ever left behind once its storyboard is removed.
         void finishAll()
         {
            storyboard.Remove(_root);
            cleanups.ForEach(x => x());
         }

         foreach (TransitionPart part in _parts)
         {
            _running.AddOrUpdate(part.Content, finishAll);
         }

         storyboard.Completed += (_, _) => finishAll();
         storyboard.Begin(_root, HandoffBehavior.SnapshotAndReplace, isControllable: true);
         LastStoryboard = storyboard;

         return storyboard;
      }

      /// <summary>
      /// +1 when the newly selected item is below the previous one, -1 above, 0
      /// when there is no previous item or it left the list (filtering).
      /// </summary>
      public static int Direction(ItemsControl list, SelectionChangedEventArgs e)
      {
         ArgumentNullException.ThrowIfNull(list);
         ArgumentNullException.ThrowIfNull(e);

         if (e.RemovedItems.Count == 0 || e.AddedItems.Count == 0)
         {
            return 0;
         }

         int from = list.Items.IndexOf(e.RemovedItems[0]);
         int to = list.Items.IndexOf(e.AddedItems[0]);
         return from < 0 || to < 0 ? 0 : Math.Sign(to - from);
      }

      /// <summary>Ends a running transition on <paramref name="content"/> at once.</summary>
      public static void Finish(FrameworkElement content)
      {
         if (_running.TryGetValue(content, out Action? finish))
         {
            finish();
         }
      }

      private static void _collect(DependencyObject parent, List<(FrameworkElement, int)> marked)
      {
         int count = VisualTreeHelper.GetChildrenCount(parent);
         for (int i = 0; i < count; i++)
         {
            if (VisualTreeHelper.GetChild(parent, i) is not FrameworkElement child || !child.IsVisible)
            {
               continue;
            }

            int tier = Motion.GetTransitionTier(child);
            if (tier < 0)
            {
               _collect(child, marked);
            }
            else if (_content(child) is FrameworkElement content)
            {
               marked.Add((content, tier));
            }
         }
      }

      /// <summary>
      /// The part that actually changes: a text box's text (not its box), a list's
      /// items (not its frame), a password control's visible box text.
      /// </summary>
      private static FrameworkElement? _content(FrameworkElement marked) => marked switch
      {
         TextBoxBase or PasswordBox => ((Control)marked).Template?.FindName("PART_ContentHost", marked) as FrameworkElement ?? marked,
         ItemsControl => _descendant(marked, x => x is ItemsPresenter) ?? marked,
         UserControl => _descendant(marked, x => x is TextBoxBase or PasswordBox) is FrameworkElement box ? _content(box) : marked,
         _ => marked,
      };

      private static FrameworkElement? _descendant(DependencyObject parent, Func<FrameworkElement, bool> match)
      {
         int count = VisualTreeHelper.GetChildrenCount(parent);
         for (int i = 0; i < count; i++)
         {
            if (VisualTreeHelper.GetChild(parent, i) is not FrameworkElement child || !child.IsVisible)
            {
               continue;
            }

            if (match(child))
            {
               return child;
            }

            if (_descendant(child, match) is FrameworkElement found)
            {
               return found;
            }
         }

         return null;
      }

      private static RenderTargetBitmap _snapshot(FrameworkElement content)
      {
         DpiScale dpi = VisualTreeHelper.GetDpi(content);
         Rect area = new(content.RenderSize);
         RenderTargetBitmap bitmap = new(
            Math.Max(1, (int)Math.Ceiling(area.Width * dpi.DpiScaleX)),
            Math.Max(1, (int)Math.Ceiling(area.Height * dpi.DpiScaleY)),
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);

         // A VisualBrush with an absolute viewbox draws the element exactly at its
         // layout rect (RenderTargetBitmap.Render alone would apply its offset).
         DrawingVisual drawing = new();
         using (DrawingContext context = drawing.RenderOpen())
         {
            context.DrawRectangle(new VisualBrush(content)
            {
               Stretch = Stretch.Fill,
               ViewboxUnits = BrushMappingMode.Absolute,
               Viewbox = area,
               ViewportUnits = BrushMappingMode.Absolute,
               Viewport = area,
            }, null, area);
         }

         bitmap.Render(drawing);
         bitmap.Freeze();
         return bitmap;
      }

      private static void _keyFrames(Storyboard storyboard, DependencyObject target, DependencyProperty property, double from, double to, TimeSpan delay, TimeSpan duration)
      {
         // Holds the start value through the stagger delay (a plain BeginTime would
         // show the end state first, then jump back).
         DoubleAnimationUsingKeyFrames animation = new();
         _ = animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
         if (delay > TimeSpan.Zero)
         {
            _ = animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(delay)));
         }

         _ = animation.KeyFrames.Add(new EasingDoubleKeyFrame(to, KeyTime.FromTimeSpan(delay + duration), _easeOut));
         Storyboard.SetTarget(animation, target);
         Storyboard.SetTargetProperty(animation, new PropertyPath(property));
         storyboard.Children.Add(animation);
      }

      private static QuinticEase _frozenEase()
      {
         QuinticEase ease = new() { EasingMode = EasingMode.EaseOut };
         ease.Freeze();
         return ease;
      }

      private sealed record TransitionPart(FrameworkElement Content, UIElement Host, AdornerLayer Layer, Rect Bounds, BitmapSource Snapshot, TimeSpan Delay);

      /// <summary>Draws the outgoing snapshot over the part, clipped to its bounds.</summary>
      private sealed class SnapshotAdorner : Adorner
      {
         private readonly Rect _bounds;

         public SnapshotAdorner(UIElement host, BitmapSource snapshot, Rect bounds)
            : base(host)
         {
            _bounds = bounds;
            IsHitTestVisible = false;
            Image = new Image
            {
               Source = snapshot,
               Stretch = Stretch.Fill,
               RenderTransform = Shift,
            };
            AddVisualChild(Image);
            Clip = new RectangleGeometry(bounds);
         }

         public Image Image { get; }

         public TranslateTransform Shift { get; } = new();

         protected override int VisualChildrenCount => 1;

         protected override Visual GetVisualChild(int index) => Image;

         protected override Size MeasureOverride(Size constraint)
         {
            Image.Measure(_bounds.Size);
            return AdornedElement.RenderSize;
         }

         protected override Size ArrangeOverride(Size finalSize)
         {
            Image.Arrange(_bounds);
            return finalSize;
         }
      }
   }
}
