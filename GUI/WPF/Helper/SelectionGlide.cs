using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// One selection highlight per list that glides from the old row to the new
   /// one instead of each row fading its own. The highlight is
   /// <c>PART_GlideHighlight</c> in the <c>GlideListBox</c> template, on a canvas
   /// behind the rows. Scrolling, resizing and filtering snap it without
   /// animating; with no visible selected row it hides. Turned on by
   /// <see cref="Motion.GlideSelectionProperty"/>.
   /// <para>
   /// The glide is a per-frame follow, not a fixed-length animation: every frame
   /// the highlight closes the same share of the distance left (time constant
   /// 25 ms, so it starts at full speed and lands softly, about 110 ms for one
   /// row). A new selection only moves the target, so a click, a tapped arrow and
   /// a held arrow all move the same way: nothing restarts, nothing snaps, and a
   /// fast run of steps is followed smoothly a fraction of a row behind.
   /// The highlight is laid out on the target row once per selection; the glide
   /// itself only changes a render transform, so its frames cost no layout.
   /// </para>
   /// </summary>
   internal sealed class SelectionGlide
   {
      private const double TIME_CONSTANT_SECONDS = 0.025;
      private const double LANDED_PIXELS = 0.5;

      private readonly ListBox _list;
      private FrameworkElement? _highlight;

      // Where the highlight is heading (the selected row), and where it is drawn.
      private Rect _shown = Rect.Empty;
      private object? _shownItem;
      private double _drawnTop = double.NaN;
      private double _drawnHeight = double.NaN;

      private bool _gliding;
      private bool _glideQueued;
      private TimeSpan _lastFrame;

      // A scroll or a change of the items since the last selection: snap, don't glide.
      private bool _snapOnly;

      private SelectionGlide(ListBox list)
      {
         _list = list;
         _list.SelectionChanged += _list_SelectionChanged;
         _list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(_list_ScrollChanged));
         ((INotifyCollectionChanged)_list.Items).CollectionChanged += (_, _) => _snapOnly = true;
         _list.Loaded += (_, _) => _list.LayoutUpdated += _list_LayoutUpdated;
         _list.Unloaded += (_, _) =>
         {
            _list.LayoutUpdated -= _list_LayoutUpdated;
            _stop();
         };
         if (_list.IsLoaded)
         {
            _list.LayoutUpdated += _list_LayoutUpdated;
         }
      }

      public static SelectionGlide Attach(ListBox list) => new(list);

      private void _list_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Selectors inside the rows (combo boxes) bubble their own changes.
         if (!ReferenceEquals(e.OriginalSource, _list) || _glideQueued)
         {
            return;
         }

         // The new row is usually already on screen and laid out: move the target
         // right away, before the rest of the page does its own work for the new
         // selection, so the highlight answers the click or key at once.
         if (_list.SelectedItem is not null
            && _list.ItemContainerGenerator.ContainerFromItem(_list.SelectedItem) is ListBoxItem { IsVisible: true, IsArrangeValid: true })
         {
            bool animateNow = !_snapOnly;
            _snapOnly = false;
            _place(animateNow);
            return;
         }

         // Otherwise let the list lay out (scroll into view, filtering) before measuring.
         _glideQueued = true;
         _ = _list.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
         {
            _glideQueued = false;

            // Measure the rows as they are now laid out (a scroll into view or a new
            // item source may still be pending).
            _list.UpdateLayout();
            bool animate = !_snapOnly;
            _snapOnly = false;
            _place(animate);
         });
      }

      private void _list_ScrollChanged(object sender, ScrollChangedEventArgs e)
      {
         if (e.VerticalChange != 0 || e.ViewportHeightChange != 0 || e.ExtentHeightChange != 0)
         {
            _snapOnly = _glideQueued;
            _place(animate: false);
         }
      }

      private void _list_LayoutUpdated(object? sender, EventArgs e)
      {
         // While gliding, the glide itself lays the canvas out every frame and
         // lands on the row as laid out then. A new selection is placed by its own
         // (queued) glide: the page's own SelectionChanged handler may lay out
         // before ours has even run.
         if (_gliding || _glideQueued || !Equals(_list.SelectedItem, _shownItem))
         {
            return;
         }

         _snapOnly = false;
         _place(animate: false);
      }

      private void _place(bool animate)
      {
         _highlight ??= _list.Template?.FindName("PART_GlideHighlight", _list) as FrameworkElement;
         if (_highlight is null || VisualTreeHelper.GetParent(_highlight) is not Visual layer)
         {
            return;
         }

         Rect? target = _list.SelectedItem is not null
            && _list.ItemContainerGenerator.ContainerFromItem(_list.SelectedItem) is ListBoxItem { IsVisible: true } container
            && container.IsDescendantOf(_list)
               ? container.TransformToVisual(layer).TransformBounds(new Rect(container.RenderSize))
               : null;

         if (target is not Rect to)
         {
            _stop();
            _highlight.Visibility = Visibility.Collapsed;
            _shown = Rect.Empty;
            _shownItem = _list.SelectedItem;
            return;
         }

         if (!animate && !_gliding && to == _shown && _highlight.Visibility == Visibility.Visible)
         {
            return;
         }

         bool glide = animate
            && Motion.Enabled
            && !_shown.IsEmpty
            && _highlight.Visibility == Visibility.Visible
            && !double.IsNaN(_drawnTop);

         _highlight.Visibility = Visibility.Visible;
         Canvas.SetLeft(_highlight, to.X);
         _highlight.Width = to.Width;
         _shown = to;
         _shownItem = _list.SelectedItem;

         if (!glide)
         {
            _stop();
            _draw(to.Y, to.Height);
            return;
         }

         // Only the target moves: a glide already running keeps its position and
         // simply heads somewhere new (redrawn now against the new layout slot).
         _draw(_drawnTop, _drawnHeight);
         if (!_gliding)
         {
            _gliding = true;
            _lastFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += _compositionTarget_Rendering;
         }
      }

      private void _compositionTarget_Rendering(object? sender, EventArgs e)
      {
         if (!_gliding || _highlight is null)
         {
            return;
         }

         // Rendering can be raised more than once for one frame.
         TimeSpan now = e is RenderingEventArgs rendering ? rendering.RenderingTime : TimeSpan.Zero;
         if (now == _lastFrame)
         {
            return;
         }

         // The first frame of a glide moves by one display frame (about 7 ms at
         // 143 Hz); a late frame (a busy page) catches up but never overshoots.
         double seconds = _lastFrame == TimeSpan.Zero ? 1.0 / 144 : Math.Clamp((now - _lastFrame).TotalSeconds, 0, 0.1);
         _lastFrame = now;
         double share = 1 - Math.Exp(-seconds / TIME_CONSTANT_SECONDS);

         double top = _drawnTop + ((_shown.Y - _drawnTop) * share);
         double height = _drawnHeight + ((_shown.Height - _drawnHeight) * share);

         if (Math.Abs(_shown.Y - top) < LANDED_PIXELS && Math.Abs(_shown.Height - height) < LANDED_PIXELS)
         {
            _stop();

            // End on the selected row as it is laid out now (rows can move during
            // a glide), never on a stale target.
            _place(animate: false);
            if (!_shown.IsEmpty)
            {
               _draw(_shown.Y, _shown.Height);
            }

            return;
         }

         _draw(top, height);
      }

      // The highlight is laid out once on its target row; where it is drawn on the
      // way there is a render transform only, so a glide frame never runs layout.
      private void _draw(double top, double height)
      {
         _drawnTop = top;
         _drawnHeight = height;

         FrameworkElement highlight = _highlight!;
         Canvas.SetTop(highlight, _shown.Y);
         highlight.Height = _shown.Height;

         if (highlight.RenderTransform is not TransformGroup { Children: [ScaleTransform scale, TranslateTransform translate] })
         {
            scale = new ScaleTransform();
            translate = new TranslateTransform();
            highlight.RenderTransformOrigin = new Point(0, 0);
            highlight.RenderTransform = new TransformGroup { Children = { scale, translate } };
         }

         translate.Y = top - _shown.Y;
         scale.ScaleY = _shown.Height > 0 ? height / _shown.Height : 1;
      }

      private void _stop()
      {
         if (_gliding)
         {
            _gliding = false;
            CompositionTarget.Rendering -= _compositionTarget_Rendering;
         }
      }
   }
}
