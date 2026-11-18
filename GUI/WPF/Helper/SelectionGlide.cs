using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// One selection highlight per list that glides from the old row to the new
   /// one (150 ms strong ease-out) instead of each row fading its own. The highlight is
   /// <c>PART_GlideHighlight</c> in the <c>GlideListBox</c> template, on a canvas
   /// behind the rows. Scrolling, resizing and filtering snap it without
   /// animating; with no visible selected row it hides. Turned on by
   /// <see cref="Motion.GlideSelectionProperty"/>.
   /// </summary>
   internal sealed class SelectionGlide
   {
      private static readonly TimeSpan _duration = TimeSpan.FromMilliseconds(150);
      private static readonly QuinticEase _easeOut = _frozenEase();

      private readonly ListBox _list;
      private FrameworkElement? _highlight;
      private Storyboard? _storyboard;
      private Rect _shown = Rect.Empty;
      private object? _shownItem;
      private bool _gliding;
      private bool _glideQueued;

      // A scroll or a change of the items since the last selection: snap, don't glide.
      private bool _snapOnly;

      private static readonly TimeSpan _rapidWindow = TimeSpan.FromMilliseconds(250);
      private DateTime _lastSelectionChange = DateTime.MinValue;

      private SelectionGlide(ListBox list)
      {
         _list = list;
         _list.SelectionChanged += _list_SelectionChanged;
         _list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(_list_ScrollChanged));
         ((INotifyCollectionChanged)_list.Items).CollectionChanged += (_, _) => _snapOnly = true;
         _list.Loaded += (_, _) => _list.LayoutUpdated += _list_LayoutUpdated;
         _list.Unloaded += (_, _) => _list.LayoutUpdated -= _list_LayoutUpdated;
         if (_list.IsLoaded)
         {
            _list.LayoutUpdated += _list_LayoutUpdated;
         }
      }

      public static SelectionGlide Attach(ListBox list) => new(list);

      private void _list_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Selectors inside the rows (combo boxes) bubble their own changes.
         if (!ReferenceEquals(e.OriginalSource, _list))
         {
            return;
         }

         // Fast clicking or a held arrow key: the highlight jumps straight to each
         // new row instead of gliding after it (a glide that is always catching up
         // makes a click look as if it had not been taken).
         DateTime now = DateTime.UtcNow;
         if (_gliding || now - _lastSelectionChange < _rapidWindow)
         {
            _snapOnly = true;
         }

         _lastSelectionChange = now;
         if (_glideQueued)
         {
            return;
         }

         // The clicked row is usually already on screen and laid out: start the glide
         // right away, before the rest of the page does its own work for the new
         // selection, so the highlight answers the click at once.
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
         // A new selection is placed by its own (queued) glide: the page's own
         // SelectionChanged handler may lay out before ours has even run.
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

         if (!animate && to == _shown && _highlight.Visibility == Visibility.Visible)
         {
            return;
         }

         bool glide = animate && Motion.Enabled && !_shown.IsEmpty && _highlight.Visibility == Visibility.Visible;

         // Start from where the highlight is drawn right now (mid-glide included),
         // not from the row it was heading to.
         double fromTop = Canvas.GetTop(_highlight);
         double fromHeight = _highlight.GetValue(FrameworkElement.HeightProperty) is double height && !double.IsNaN(height)
            ? height
            : _highlight.ActualHeight;

         _stop();
         _highlight.Visibility = Visibility.Visible;
         Canvas.SetLeft(_highlight, to.X);
         Canvas.SetTop(_highlight, to.Y);
         _highlight.Width = to.Width;
         _highlight.Height = to.Height;
         _shown = to;
         _shownItem = _list.SelectedItem;

         if (!glide || double.IsNaN(fromTop) || (fromTop == to.Y && fromHeight == to.Height))
         {
            return;
         }

         // The base values are already the target: the storyboard only runs from
         // where the highlight was and lets go at the end (FillBehavior.Stop).
         _gliding = true;
         Storyboard storyboard = new() { FillBehavior = FillBehavior.Stop };
         _add(storyboard, Canvas.TopProperty, fromTop, to.Y);
         _add(storyboard, FrameworkElement.HeightProperty, fromHeight, to.Height);
         storyboard.Completed += (_, _) =>
         {
            // A newer glide or a snap took over: this one has nothing left to say.
            if (_storyboard != storyboard)
            {
               return;
            }

            _stop();

            // End on the selected row as it is laid out now (rows can move during
            // a glide), never on a stale target.
            _place(animate: false);
         };
         _storyboard = storyboard;
         storyboard.Begin(_highlight, HandoffBehavior.SnapshotAndReplace, isControllable: true);
      }

      private void _add(Storyboard storyboard, DependencyProperty property, double from, double to)
      {
         DoubleAnimation animation = new(from, to, _duration) { EasingFunction = _easeOut };
         Storyboard.SetTarget(animation, _highlight);
         Storyboard.SetTargetProperty(animation, new PropertyPath(property));
         storyboard.Children.Add(animation);
      }

      private void _stop()
      {
         _gliding = false;
         if (_storyboard is not null && _highlight is not null)
         {
            _storyboard.Remove(_highlight);
         }

         _storyboard = null;
      }

      private static QuinticEase _frozenEase()
      {
         QuinticEase ease = new() { EasingMode = EasingMode.EaseOut };
         ease.Freeze();
         return ease;
      }
   }
}
