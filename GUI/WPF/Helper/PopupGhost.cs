using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// The closing half of a popup's motion. A WPF popup's window is destroyed the
   /// moment it closes, so it cannot fade itself out without holding the close
   /// back (and input with it). Instead, when a drop-down, menu, calendar,
   /// context menu or tooltip closes, a copy of its last frame (vector drawing,
   /// nothing rasterized) is shown in a click-through, never-activated popup at
   /// the same place and fades out and rises 4 px (<see cref="Motion.Leave"/>):
   /// the opening drop reversed. The real popup is already gone, so the next
   /// click or key goes straight to the window.
   /// <para>
   /// Never for a popup showing a revealed password (none of ours does), and
   /// skipped when Windows animations are off.
   /// </para>
   /// </summary>
   internal static class PopupGhost
   {
      private static readonly DependencyProperty _placeProperty = DependencyProperty.RegisterAttached(
         "Place", typeof(Place), typeof(PopupGhost), new PropertyMetadata(null));

      private static bool _initialized;

      /// <summary>Hooks every context menu and tooltip (they have no template popup to mark).</summary>
      public static void Initialize()
      {
         if (_initialized)
         {
            return;
         }

         _initialized = true;

         // Smooth the first time too: build one invisible, click-through 1 px copy
         // when the app is first idle, so the first real close pays no setup cost.
         _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, static () =>
         {
            if (!Motion.Enabled)
            {
               return;
            }

            DrawingPresenter blank = new(new DrawingGroup(), new Size(1, 1));
            Popup warm = new()
            {
               AllowsTransparency = true,
               Placement = PlacementMode.Absolute,
               PopupAnimation = PopupAnimation.None,
               Focusable = false,
               IsHitTestVisible = false,
               Child = blank,
            };
            warm.IsOpen = true;
            if (PresentationSource.FromVisual(blank) is HwndSource source)
            {
               NativeMethods.MakeClickThrough(source.Handle);
            }

            _ = VectorSnapshot.Of(blank);
            _ = Motion.Leave(blank, () => warm.IsOpen = false);
         });

         EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent, new RoutedEventHandler(static (sender, _) =>
         {
            if (sender is ContextMenu menu)
            {
               _watchClose(menu, ContextMenu.IsOpenProperty, menu, () => menu.PlacementTarget);
            }
         }));
         EventManager.RegisterClassHandler(typeof(ToolTip), ToolTip.OpenedEvent, new RoutedEventHandler(static (sender, _) =>
         {
            if (sender is ToolTip tip)
            {
               _watchClose(tip, ToolTip.IsOpenProperty, tip, () => tip.PlacementTarget);
            }
         }));
      }

      /// <summary>For a template popup (<see cref="Motion.FadeOnCloseProperty"/>).</summary>
      public static void Attach(Popup popup)
      {
         ArgumentNullException.ThrowIfNull(popup);

         popup.Opened += (_, _) =>
         {
            if (popup.Child is FrameworkElement child)
            {
               // The popup element itself sits in the window's tree: it anchors the copy.
               _watchClose(popup, Popup.IsOpenProperty, child, () => popup.PlacementTarget ?? popup);
            }
         };
      }

      // WPF takes a popup's window down a moment after IsOpen turns false. The copy
      // is made right then, while the popup is still on screen (exact place, laid
      // out content), so it is up before the real one goes: no blank frame.
      private static void _watchClose(DependencyObject owner, DependencyProperty isOpen, FrameworkElement content, Func<UIElement?> anchor)
      {
         DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(isOpen, owner.GetType());
         EventHandler? onChanged = null;
         onChanged = (_, _) =>
         {
            if (owner.GetValue(isOpen) is true)
            {
               return;
            }

            descriptor.RemoveValueChanged(owner, onChanged);
            _record(content, anchor());
            _fadeOut(content);
         };
         descriptor.AddValueChanged(owner, onChanged);
      }

      private static void _record(FrameworkElement content, UIElement? anchor)
      {
         // (A popup element reports IsVisible false: it draws nothing in the window itself.)
         if (anchor is null
            || PresentationSource.FromVisual(content) is not HwndSource { RootVisual: Visual root }
            || PresentationSource.FromVisual(anchor) is null)
         {
            return;
         }

         // The popup window's top-left (not the content's: it may still be mid
         // drop-in), and the content's layout offset inside it.
         Point popupScreen = ((UIElement)root).PointToScreen(default);
         Point anchorScreen = anchor.PointToScreen(default);
         double scale = VisualTreeHelper.GetDpi(anchor).DpiScaleX;
         Vector offset = VisualTreeHelper.GetOffset(content);
         content.SetValue(_placeProperty, new Place(anchor, new Vector((popupScreen.X - anchorScreen.X) / scale, (popupScreen.Y - anchorScreen.Y) / scale), offset, content.RenderSize));
      }

      private static void _fadeOut(FrameworkElement content)
      {
         if (content.GetValue(_placeProperty) is not Place place)
         {
            return;
         }

         content.ClearValue(_placeProperty);
         if (!Motion.Enabled
            || PresentationSource.FromVisual(place.Anchor) is null
            || place.Size.Width <= 0
            || place.Size.Height <= 0
            || ContentTransition.RevealsSecret(content))
         {
            return;
         }

         // Closing detaches the content and zeroes its size; its drawing is intact.
         DrawingPresenter copy = new(VectorSnapshot.Of(content).Drawing, place.Size)
         {
            Margin = new Thickness(place.ContentOffset.X, place.ContentOffset.Y, 0, 0),
         };
         Popup ghost = new()
         {
            AllowsTransparency = true,
            Placement = PlacementMode.Relative,
            PlacementTarget = place.Anchor,
            HorizontalOffset = place.Offset.X,
            VerticalOffset = place.Offset.Y,
            PopupAnimation = PopupAnimation.None,
            StaysOpen = true,
            Focusable = false,
            IsHitTestVisible = false,
            Child = copy,
         };

         ghost.IsOpen = true;
         if (PresentationSource.FromVisual(copy) is HwndSource source)
         {
            NativeMethods.MakeClickThrough(source.Handle);
         }

         _ = Motion.Leave(copy, () => ghost.IsOpen = false);
      }

      private sealed record Place(UIElement Anchor, Vector Offset, Vector ContentOffset, Size Size);
   }
}
