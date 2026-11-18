using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// A single-selection list selects the row that was pressed, and only that row.
   /// <para>
   /// A stock <see cref="ListBox"/> captures the mouse on every press and, while it
   /// holds it, follows the pointer: the row under the pointer is selected as it
   /// moves (drag-select), and a pointer above or below the list moves the
   /// selection and the focus to the next row (auto-scroll). Taking the capture
   /// already reads where the pointer is now, not where it was pressed. When the
   /// window is busy for a moment (a selection redraws the details pane) and the
   /// pointer is already on its way to the next target, the press therefore
   /// selects the clicked row, then a neighbour, or leaves the focus on one: the
   /// click looks lost.
   /// </para>
   /// <para>
   /// While the list holds the capture inside its own press handling, pointer
   /// moves are not given to it, and the capture is released as soon as the press
   /// is handled. Keyboard selection, the focus visual and double-clicks are
   /// unchanged. Turned on for every list by the base list style.
   /// </para>
   /// </summary>
   internal static class PressSelection
   {
      public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
         "IsEnabled", typeof(bool), typeof(PressSelection), new PropertyMetadata(false, _onIsEnabledChanged));

      public static bool GetIsEnabled(DependencyObject element)
      {
         ArgumentNullException.ThrowIfNull(element);
         return (bool)element.GetValue(IsEnabledProperty);
      }

      public static void SetIsEnabled(DependencyObject element, bool value)
      {
         ArgumentNullException.ThrowIfNull(element);
         element.SetValue(IsEnabledProperty, value);
      }

      private static readonly MouseEventHandler _previewMouseMove = _list_PreviewMouseMove;
      private static readonly MouseButtonEventHandler _mouseDown = _list_MouseDown;

      private static void _onIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         if (d is not ListBox list)
         {
            return;
         }

         list.RemoveHandler(UIElement.PreviewMouseMoveEvent, _previewMouseMove);
         list.RemoveHandler(UIElement.MouseDownEvent, _mouseDown);
         if (e.NewValue is true)
         {
            list.AddHandler(UIElement.PreviewMouseMoveEvent, _previewMouseMove, handledEventsToo: true);
            // The press bubbles up from the row, which has handled it by then.
            list.AddHandler(UIElement.MouseDownEvent, _mouseDown, handledEventsToo: true);
         }
      }

      private static bool _ownsCapture(ListBox list) => list.SelectionMode == SelectionMode.Single && Mouse.Captured == list;

      // The only time a single-selection list holds the capture now is inside its
      // own press handling: keep its drag-select and auto-scroll out of it.
      private static void _list_PreviewMouseMove(object sender, MouseEventArgs e)
      {
         if (sender is ListBox list && _ownsCapture(list))
         {
            e.Handled = true;
         }
      }

      private static void _list_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (e.ChangedButton != MouseButton.Left || sender is not ListBox list || !_ownsCapture(list))
         {
            return;
         }

         list.ReleaseMouseCapture();

         // Should a neighbour still have taken the focus, give it back to the
         // pressed row (its focus visual must not sit on another row).
         if (ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) is ListBoxItem pressed
            && !pressed.IsKeyboardFocusWithin
            && Keyboard.FocusedElement is ListBoxItem focused
            && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(focused), list))
         {
            _ = pressed.Focus();
         }
      }
   }
}
