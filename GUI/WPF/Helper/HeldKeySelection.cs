using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Keeps a held arrow key responsive in a list whose selection fills a details
   /// pane. The list (and its view model binding and highlight) follows every
   /// step at once; the pane, the costly part, is filled only for the row the
   /// keys stop on.
   /// <para>
   /// A selection made by a navigation key that repeats (Windows auto-repeat), or
   /// that comes within 100 ms of the previous key step, is <i>deferred</i>: the
   /// pane is filled 90 ms after the last step, or at once when the key is
   /// released. Any other key or a mouse press anywhere in the window fills it
   /// first, so a command, an edit or a click never acts on a pane showing an
   /// older row.
   /// </para>
   /// <para>
   /// A single key press and a click fill the pane at once, in the same frame as
   /// the highlight moves: the highlight's glide and the pane's crossfade then both
   /// start on the next frame and run with nothing heavy behind them. (Filling one
   /// frame later would start the glide and then stall it under the rebuild.)
   /// </para>
   /// <para>
   /// A waiting fill is dropped, never run, once the list is unloaded or its
   /// window closes: the window clears the pane on close and nothing may fill it
   /// again afterwards.
   /// </para>
   /// </summary>
   internal sealed class HeldKeySelection
   {
      private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(90);
      private static readonly TimeSpan _rapidStep = TimeSpan.FromMilliseconds(100);

      private readonly ListBox _list;
      private readonly Func<bool> _fill;
      private readonly DispatcherTimer _timer;
      private Window? _window;

      private bool _inKeyStep;
      private bool _repeating;
      private DateTime _lastKeyStep = DateTime.MinValue;

      /// <param name="list">The list the keys move through.</param>
      /// <param name="fill">
      /// Fills the pane for the list's current selection. Returns false when it
      /// cannot right now (the window is busy); it is then tried again a moment
      /// later instead of leaving the pane on an older row.
      /// </param>
      public HeldKeySelection(ListBox list, Func<bool> fill)
      {
         ArgumentNullException.ThrowIfNull(list);
         ArgumentNullException.ThrowIfNull(fill);

         _list = list;
         _fill = fill;
         _timer = new DispatcherTimer(DispatcherPriority.Input, list.Dispatcher) { Interval = _settle };
         _timer.Tick += (_, _) => Flush();

         _list.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(_list_PreviewKeyDown), handledEventsToo: true);
         // The list moves the selection in its own (class) KeyDown handling; this runs after it.
         _list.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, _) => _inKeyStep = false), handledEventsToo: true);
         _list.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(_list_KeyUp), handledEventsToo: true);
         _list.Loaded += (_, _) => _hookWindow();
         _list.Unloaded += (_, _) => Cancel();
      }

      /// <summary>
      /// Call from the list's SelectionChanged: true when this step only moves the
      /// list and the pane is filled later (by the fill given at creation).
      /// </summary>
      public bool Defer()
      {
         _timer.Stop();

         if (!_inKeyStep || !_repeating)
         {
            return false;
         }

         _timer.Start();
         return true;
      }

      /// <summary>Fills the pane now if a fill is waiting.</summary>
      public void Flush()
      {
         if (!_timer.IsEnabled)
         {
            return;
         }

         _timer.Stop();
         if (!_fill())
         {
            _timer.Start();
         }
      }

      /// <summary>Drops a waiting fill without running it.</summary>
      public void Cancel()
      {
         _timer.Stop();
         _inKeyStep = false;
      }

      private static bool _isNavigationKey(Key key) => key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End;

      private void _list_PreviewKeyDown(object sender, KeyEventArgs e)
      {
         if (!_isNavigationKey(e.Key) || Keyboard.Modifiers != ModifierKeys.None)
         {
            _inKeyStep = false;
            Flush();
            return;
         }

         DateTime now = DateTime.UtcNow;
         _repeating = e.IsRepeat || now - _lastKeyStep < _rapidStep;
         _lastKeyStep = now;
         _inKeyStep = true;
      }

      private void _list_KeyUp(object sender, KeyEventArgs e)
      {
         if (_isNavigationKey(e.Key))
         {
            _inKeyStep = false;
            Flush();
         }
      }

      private bool _isInList(DependencyObject? element)
      {
         for (; element is not null; element = element is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element))
         {
            if (ReferenceEquals(element, _list))
            {
               return true;
            }
         }

         return false;
      }

      private void _hookWindow()
      {
         Window? window = Window.GetWindow(_list);
         if (window is null || ReferenceEquals(window, _window))
         {
            return;
         }

         _window = window;
         window.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler((_, _) =>
         {
            // A key step whose KeyDown never reached the list must not turn this
            // click into a deferred one.
            _inKeyStep = false;
            Flush();
         }), handledEventsToo: true);
         window.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((_, e) =>
         {
            // A navigation key aimed at this list is its own step; any other key,
            // or a navigation key for another control, acts on the pane: fill it first.
            if (!_isNavigationKey(e.Key) || !_isInList(e.OriginalSource as DependencyObject))
            {
               Flush();
            }
         }), handledEventsToo: true);
         window.Deactivated += (_, _) => Flush();
         // Closing can still be cancelled (unsaved changes), so the pane is brought
         // up to date; once closed, nothing may fill it again.
         window.Closing += (_, _) => Flush();
         window.Closed += (_, _) => Cancel();
      }
   }
}
