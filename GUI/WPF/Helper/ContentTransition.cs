using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Upsilon.Apps.Passkey.GUI.WPF.Views.Controls;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Moves only what changed when a service or account is selected.
   /// <para>
   /// Parts are marked with <see cref="Motion.TransitionTierProperty"/>: a field's
   /// text, a combo box's value, each row of a list (account rows, quality ticks,
   /// history rows). <see cref="Capture"/> copies every part's drawing (the vector
   /// instructions WPF already keeps for it: glyph runs, shapes) just before the
   /// selection changes; after the change <see cref="Play"/> lays the page out once
   /// and compares those drawings with the new ones, before the next frame is drawn.
   /// Identical parts are left alone: no overlay, no motion. Nothing is rasterized
   /// and no pixels are compared, so a switch costs about a millisecond on top of
   /// the page's own layout.
   /// A part that changed hands over quickly and lopsidedly: the old value fades
   /// out over 100 ms (ease-in, so it drops late and fast) while the new one fades
   /// in from 70 to 190 ms (ease-out), both sliding 10 px in the direction of
   /// travel. Where both are visible the weaker one is at most 0.3 opacity (never
   /// a muddy 50/50 frame of two texts).
   /// </para>
   /// <para>
   /// The old drawings sit in the window's adorner layer, clipped to each part's
   /// visible bounds, so nothing slides over a frame. Frames, captions and buttons
   /// are never parts. Cosmetic only: nothing waits on it, and with Windows
   /// animations off nothing is captured.
   /// </para>
   /// </summary>
   internal sealed class ContentTransition
   {
      private const double SLIDE_DISTANCE = 10;
      private const int MAX_STAGGERED_ROWS = 5;
      private static readonly TimeSpan _outDuration = TimeSpan.FromMilliseconds(100);
      private static readonly TimeSpan _inStart = TimeSpan.FromMilliseconds(70);
      private static readonly TimeSpan _inDuration = TimeSpan.FromMilliseconds(120);
      private static readonly TimeSpan _tierStagger = TimeSpan.FromMilliseconds(30);
      private static readonly TimeSpan _rowStagger = TimeSpan.FromMilliseconds(20);
      private static readonly QuadraticEase _easeIn = _frozen(new QuadraticEase { EasingMode = EasingMode.EaseIn });
      private static readonly CubicEase _easeOut = _frozen(new CubicEase { EasingMode = EasingMode.EaseOut });

      // One transition at a time: a new selection finishes the running one at once.
      private static ContentTransition? _current;

      // Selections closer together than this count as rapid navigation (no motion).
      private static readonly TimeSpan _rapidWindow = TimeSpan.FromMilliseconds(250);
      private static DateTime _lastRequest = DateTime.MinValue;

      private readonly FrameworkElement _root;
      private readonly Dictionary<string, Snapshot> _old;
      private readonly List<DrawingAdorner> _adorners = [];
      private readonly List<(FrameworkElement Content, object Opacity, Transform Transform, Geometry? Clip)> _touched = [];
      private Storyboard? _storyboard;
      private bool _finished;

      private ContentTransition(FrameworkElement root, Dictionary<string, Snapshot> old)
      {
         _root = root;
         _old = old;
      }

      /// <summary>
      /// Snapshots every marked part under <paramref name="root"/>. Returns null
      /// when there is nothing to animate (animations off, not shown yet).
      /// </summary>
      public static ContentTransition? Capture(FrameworkElement root)
      {
         ArgumentNullException.ThrowIfNull(root);

         // A selection while the previous transition is still playing, or within
         // 250 ms of the previous selection (fast clicking, a held arrow key), ends
         // that one at once and switches without motion: snapshotting and
         // animating every step only piles up work on the UI thread, and the list
         // feels stuck. The next unhurried selection animates again.
         DateTime now = DateTime.UtcNow;
         bool rapid = _current is { _finished: false } || now - _lastRequest < _rapidWindow;
         _lastRequest = now;
         _current?._finish();

         if (rapid || !Motion.Enabled || !root.IsLoaded || !root.IsVisible)
         {
            return null;
         }

         Dictionary<string, Snapshot> old = [];
         foreach (Part part in _collect(root))
         {
            if (_visibleBounds(part.Content, root) is Rect visible)
            {
               old[part.Key] = new Snapshot(part, VectorSnapshot.Of(part.Content), part.Content.TransformToAncestor(root).TransformBounds(new Rect(part.Content.RenderSize)), visible);
            }
         }

         return old.Count == 0 ? null : new ContentTransition(root, old);
      }

      /// <summary>
      /// Runs the transition. <paramref name="direction"/> &gt; 0: the new item is
      /// below the old one, content travels left (new comes in from the right);
      /// &lt; 0: the opposite; 0: fade only (filtering, undo).
      /// </summary>
      public void Play(int direction)
      {
         _current = this;

         // Everything is decided before the next frame is drawn: an unchanged part
         // never gets an overlay, not even for one frame (an overlay over unchanged
         // text read as a vibration).
         _start(Math.Sign(direction));
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

      private void _start(int direction)
      {
         if (_finished)
         {
            return;
         }

         _root.UpdateLayout();

         Dictionary<string, Part> current = _collect(_root).ToDictionary(x => x.Key);
         int firstTier = _old.Values.Select(x => x.Part.Tier).Concat(current.Values.Select(x => x.Tier)).DefaultIfEmpty(0).Min();
         Storyboard storyboard = new();

         foreach (string key in _old.Keys.Union(current.Keys))
         {
            Snapshot? before = _old.GetValueOrDefault(key);
            Part? after = current.GetValueOrDefault(key);

            // Unchanged (same place, same drawing): nothing is drawn over it, nothing
            // moves. Only what is visibly drawn counts, so a field that just got
            // narrower (a scrollbar appeared next to it) with the same text is
            // unchanged too.
            if (before is not null && after is not null
               && before.Bounds.TopLeft == after.Content.TransformToAncestor(_root).Transform(default)
               && before.Drawing.Matches(VectorSnapshot.Of(after.Content)))
            {
               continue;
            }

            DrawingAdorner? adorner = before is null ? null : _addAdorner(before);

            Part reference = after ?? before!.Part;
            TimeSpan delay = ((reference.Tier - firstTier) * _tierStagger) + (Math.Min(reference.Row, MAX_STAGGERED_ROWS) * _rowStagger);

            if (adorner is not null)
            {
               _keyFrames(storyboard, adorner.Host, UIElement.OpacityProperty, delay, (1, TimeSpan.Zero, null), (0, _outDuration, _easeIn));
               _keyFrames(storyboard, adorner.Shift, TranslateTransform.XProperty, delay, (0, TimeSpan.Zero, null), (-SLIDE_DISTANCE * direction, _outDuration, _easeIn));
            }

            if (after is not null && _visibleBounds(after.Content, _root) is not null)
            {
               FrameworkElement content = after.Content;
               if (!_touched.Any(x => x.Content == content))
               {
                  _touch(content);
               }

               TranslateTransform shift = new();
               TranslateTransform clipShift = new();
               content.RenderTransform = shift;
               content.Clip = new RectangleGeometry(new Rect(0, -10000, Math.Max(1, content.ActualWidth), 20000)) { Transform = clipShift };

               TimeSpan inStart = delay + _inStart;
               _keyFrames(storyboard, content, UIElement.OpacityProperty, inStart, (0, TimeSpan.Zero, null), (1, _inDuration, _easeOut));
               if (after.Companion is { IsVisible: true } companion && !_touched.Any(x => x.Content == companion))
               {
                  _touch(companion);
                  _keyFrames(storyboard, companion, UIElement.OpacityProperty, inStart, (0, TimeSpan.Zero, null), (1, _inDuration, _easeOut));
               }
               _keyFrames(storyboard, shift, TranslateTransform.XProperty, inStart, (SLIDE_DISTANCE * direction, TimeSpan.Zero, null), (0, _inDuration, _easeOut));
               _keyFrames(storyboard, clipShift, TranslateTransform.XProperty, inStart, (-SLIDE_DISTANCE * direction, TimeSpan.Zero, null), (0, _inDuration, _easeOut));
            }
         }

         if (storyboard.Children.Count == 0)
         {
            _finish();
            return;
         }

         _storyboard = storyboard;
         storyboard.Completed += (_, _) => _finish();
         storyboard.Begin(_root, HandoffBehavior.SnapshotAndReplace, isControllable: true);
      }

      private void _finish()
      {
         if (_finished)
         {
            return;
         }

         _finished = true;
         _storyboard?.Remove(_root);
         foreach (DrawingAdorner adorner in _adorners.ToList())
         {
            _remove(adorner);
         }

         foreach ((FrameworkElement content, object opacity, Transform transform, Geometry? clip) in _touched)
         {
            _restoreOpacity(content, opacity);
            content.RenderTransform = transform;
            content.Clip = clip;
         }

         _touched.Clear();

         // Drop the drawings now rather than whenever this object is collected.
         _old.Clear();
         _adorners.Clear();
         if (_current == this)
         {
            _current = null;
         }
      }

      private void _touch(FrameworkElement content)
         => _touched.Add((content, content.ReadLocalValue(UIElement.OpacityProperty), content.RenderTransform, content.Clip));

      private static void _restoreOpacity(FrameworkElement content, object opacity)
      {
         if (opacity == DependencyProperty.UnsetValue)
         {
            content.ClearValue(UIElement.OpacityProperty);
         }
         else
         {
            content.Opacity = (double)opacity;
         }
      }

      private DrawingAdorner _addAdorner(Snapshot snapshot)
      {
         DrawingAdorner adorner = new(_root, snapshot);
         AdornerLayer.GetAdornerLayer(_root)?.Add(adorner);
         _adorners.Add(adorner);
         return adorner;
      }

      private void _remove(DrawingAdorner? adorner)
      {
         if (adorner is null || !_adorners.Remove(adorner))
         {
            return;
         }

         AdornerLayer.GetAdornerLayer(_root)?.Remove(adorner);
      }

      /// <summary>Every marked part under <paramref name="root"/>, keyed by its place in the tree.</summary>
      private static List<Part> _collect(FrameworkElement root)
      {
         List<Part> parts = [];
         _collect(root, string.Empty, parts);
         return parts;
      }

      private static void _collect(DependencyObject parent, string path, List<Part> parts)
      {
         int count = VisualTreeHelper.GetChildrenCount(parent);
         for (int i = 0; i < count; i++)
         {
            if (VisualTreeHelper.GetChild(parent, i) is not FrameworkElement child || !child.IsVisible)
            {
               continue;
            }

            string key = $"{path}/{i}";
            int tier = Motion.GetTransitionTier(child);
            if (tier < 0)
            {
               _collect(child, key, parts);
            }
            else if (_revealsSecret(child))
            {
               // A password shown in clear is never copied: that part just
               // switches without motion.
            }
            else if (child is ItemsControl list)
            {
               // Lists move row by row.
               for (int row = 0; row < list.Items.Count; row++)
               {
                  if (list.ItemContainerGenerator.ContainerFromIndex(row) is FrameworkElement { IsVisible: true } container
                     && !_revealsSecret(container))
                  {
                     FrameworkElement content = container is ContentPresenter presenter
                        ? presenter
                        : _descendant(container, x => x is ContentPresenter) ?? container;
                     parts.Add(new Part($"{key}#{row}", content, tier, row));
                  }
               }
            }
            else if (_content(child) is FrameworkElement content)
            {
               parts.Add(new Part(key, content, tier, 0, _placeholder(child)));
            }
         }
      }

      /// <summary>True when <paramref name="element"/> is, or holds, a password box showing its password.</summary>
      internal static bool RevealsSecret(DependencyObject element) => _revealsSecret(element);

      private static bool _revealsSecret(DependencyObject element)
      {
         if (element is VisiblePasswordBox { IsRevealed: true })
         {
            return true;
         }

         int count = VisualTreeHelper.GetChildrenCount(element);
         for (int i = 0; i < count; i++)
         {
            if (_revealsSecret(VisualTreeHelper.GetChild(element, i)))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>A text box's placeholder (the template's <c>placeholder</c> part), if it has one.</summary>
      private static FrameworkElement? _placeholder(FrameworkElement marked)
         => marked is TextBoxBase box ? box.Template?.FindName("placeholder", box) as FrameworkElement : null;

      /// <summary>
      /// The piece that actually changes: a text box's text (not its box), a combo
      /// box's value (not its arrow), a password control's visible text.
      /// </summary>
      private static FrameworkElement? _content(FrameworkElement marked) => marked switch
      {
         TextBoxBase or PasswordBox => ((Control)marked).Template?.FindName("PART_ContentHost", marked) as FrameworkElement ?? marked,
         ComboBox => ((Control)marked).Template?.FindName("contentPresenter", marked) as FrameworkElement ?? marked,
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

      /// <summary>
      /// The part's bounds in <paramref name="root"/> coordinates, cut to what its
      /// scroll viewers show; null when nothing of it is visible.
      /// </summary>
      private static Rect? _visibleBounds(FrameworkElement content, FrameworkElement root)
      {
         if (content.ActualWidth < 1 || content.ActualHeight < 1 || !content.IsDescendantOf(root))
         {
            return null;
         }

         Rect visible = content.TransformToAncestor(root).TransformBounds(new Rect(content.RenderSize));
         for (DependencyObject? parent = VisualTreeHelper.GetParent(content); parent is not null && parent != root; parent = VisualTreeHelper.GetParent(parent))
         {
            if (parent is ScrollContentPresenter viewport)
            {
               visible.Intersect(viewport.TransformToAncestor(root).TransformBounds(new Rect(viewport.RenderSize)));
            }
         }

         return visible.IsEmpty || visible.Width < 1 || visible.Height < 1 ? null : visible;
      }

      /// <summary>
      /// A bitmap of <paramref name="element"/> at its device DPI, laid out as it is
      /// now. Only for the theme crossfade (once per theme change), never per selection.
      /// </summary>
      internal static BitmapSource Render(FrameworkElement element) => _render(element);

      private static RenderTargetBitmap _render(FrameworkElement content)
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

      private static void _keyFrames(Storyboard storyboard, DependencyObject target, DependencyProperty property, TimeSpan start, (double Value, TimeSpan At, IEasingFunction? Ease) from, (double Value, TimeSpan Duration, IEasingFunction Ease) to)
      {
         // Holds the start value until the part's turn (a plain BeginTime would show
         // the end state first, then jump back).
         DoubleAnimationUsingKeyFrames animation = new();
         _ = animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from.Value, KeyTime.FromTimeSpan(from.At)));
         if (start > TimeSpan.Zero)
         {
            _ = animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from.Value, KeyTime.FromTimeSpan(start)));
         }

         _ = animation.KeyFrames.Add(new EasingDoubleKeyFrame(to.Value, KeyTime.FromTimeSpan(start + to.Duration), to.Ease));
         Storyboard.SetTarget(animation, target);
         Storyboard.SetTargetProperty(animation, new PropertyPath(property));
         storyboard.Children.Add(animation);
      }

      private static T _frozen<T>(T ease) where T : Freezable
      {
         ease.Freeze();
         return ease;
      }

      // Companion: drawn in place of the content when it is empty (a text box's
      // placeholder); it fades in with the new value instead of showing at once over
      // the old one.
      private sealed record Part(string Key, FrameworkElement Content, int Tier, int Row, FrameworkElement? Companion = null);

      private sealed record Snapshot(Part Part, VectorSnapshot Drawing, Rect Bounds, Rect Visible);

      /// <summary>Draws one outgoing part over its place, clipped to what was visible of it.</summary>
      private sealed class DrawingAdorner : Adorner
      {
         private readonly Rect _bounds;

         public DrawingAdorner(UIElement root, Snapshot snapshot)
            : base(root)
         {
            _bounds = snapshot.Bounds;
            // Never in the way of a click: the rows under the old drawing must take it.
            IsHitTestVisible = false;
            Host = new DrawingHost(snapshot.Drawing.Drawing)
            {
               RenderTransform = Shift,
               IsHitTestVisible = false,
            };
            AddVisualChild(Host);
            Clip = new RectangleGeometry(snapshot.Visible);
         }

         public FrameworkElement Host { get; }

         public TranslateTransform Shift { get; } = new();

         protected override int VisualChildrenCount => 1;

         protected override Visual GetVisualChild(int index) => Host;

         protected override Size MeasureOverride(Size constraint)
         {
            Host.Measure(_bounds.Size);
            return AdornedElement.RenderSize;
         }

         protected override Size ArrangeOverride(Size finalSize)
         {
            Host.Arrange(_bounds);
            return finalSize;
         }
      }

      /// <summary>Replays a copied drawing at its own origin.</summary>
      private sealed class DrawingHost(Drawing drawing) : FrameworkElement
      {
         protected override void OnRender(DrawingContext drawingContext) => drawingContext.DrawDrawing(drawing);
      }
   }
}
