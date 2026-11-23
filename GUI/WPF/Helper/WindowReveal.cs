using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Opens a window without the usual pop: the window is cloaked by the
   /// compositor (shown to WPF, invisible on screen) until its first frame is
   /// fully drawn, so it never appears half built or as a blank flash. Its
   /// content starts at opacity 0 on the window's own background colour, and the
   /// zoom and fade (<see cref="Motion.Open"/>) start on the first frame that is
   /// actually on screen. With Windows animations off it still waits for the
   /// first frame, then simply appears.
   /// <para>
   /// The surface under the content is cleared to the window's background colour
   /// (not white), so the fade never flashes another colour, in either theme.
   /// </para>
   /// </summary>
   internal static class WindowReveal
   {
      // Never leave a window invisible: reveal it by then even without a frame.
      private static readonly TimeSpan _backstop = TimeSpan.FromMilliseconds(600);

      private static readonly DependencyPropertyDescriptor _background =
         DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(Window));

      /// <summary>Call on SourceInitialized (handle created, not yet shown).</summary>
      public static void Prepare(Window window)
      {
         ArgumentNullException.ThrowIfNull(window);

         IntPtr handle = new WindowInteropHelper(window).Handle;
         if (handle == IntPtr.Zero || HwndSource.FromHwnd(handle) is not HwndSource source)
         {
            return;
         }

         _clearToBackground(window, source);
         EventHandler onBackground = (_, _) => _clearToBackground(window, source);
         _background.AddValueChanged(window, onBackground);
         window.Closed += (_, _) => _background.RemoveValueChanged(window, onBackground);

         if (!NativeMethods.Cloak(handle, true))
         {
            return;
         }

         window.SetValue(_holdsOpenProperty, true);

         bool revealed = false;
         FrameworkElement? root = null;
         DispatcherTimer backstop = new(DispatcherPriority.Background, window.Dispatcher) { Interval = _backstop };

         void Reveal()
         {
            if (revealed)
            {
               return;
            }

            revealed = true;
            backstop.Stop();
            _ = NativeMethods.Cloak(handle, false);
            if (root is not null)
            {
               root.ClearValue(UIElement.OpacityProperty);
               if (Motion.Enabled)
               {
                  _ = Motion.Open(root);
               }
            }
         }

         // The content waits at opacity 0 (on the background colour) for the reveal.
         // Hidden now, before the first frame: Loaded comes after that frame is drawn,
         // and a reveal showing that full frame first, then restarting the zoom from
         // invisible, reads as a flash.
         if (Motion.Enabled && window.Content is FrameworkElement content)
         {
            root = content;
            root.Opacity = 0;
         }
         window.ContentRendered += (_, _) => Reveal();
         window.Closed += (_, _) => backstop.Stop();
         backstop.Tick += (_, _) => Reveal();
         backstop.Start();
      }

      /// <summary>True when <see cref="Prepare"/> runs the window's opening animation itself (at the reveal).</summary>
      public static bool HoldsOpen(Window window) => window.GetValue(_holdsOpenProperty) is true;

      private static readonly DependencyProperty _holdsOpenProperty = DependencyProperty.RegisterAttached(
         "HoldsOpen", typeof(bool), typeof(WindowReveal), new PropertyMetadata(false));

      private static void _clearToBackground(Window window, HwndSource source)
      {
         if (source.CompositionTarget is HwndTarget target && window.Background is SolidColorBrush { Color: var color })
         {
            target.BackgroundColor = color;
         }
      }
   }
}
