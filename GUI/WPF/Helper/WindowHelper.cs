using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Window chrome: wait cursor, immersive title bar, tab order, and closing a modal
   /// vault window when the session ends.
   /// </summary>
   internal static class WindowHelper
   {
      /// <summary>
      /// Wait-cursor convention used instead of a dedicated IsBusy DP on every window.
      /// </summary>
      public static bool GetIsBusy(this Window window)
      {
         return window.Cursor == Cursors.Wait;
      }

      public static void SetIsBusy(this Window window, bool isBusy)
      {
         window.Cursor = isBusy ? Cursors.Wait : Cursors.Arrow;
      }

      public static bool GetIsBusy(this UserControl control)
      {
         return Window.GetWindow(control).GetIsBusy();
      }

      /// <summary>
      /// Applies immersive title-bar coloring and a depth-first tab order after Loaded
      /// (the HWND is not available in the constructor).
      /// </summary>
      public static void PostLoadSetup(this Window window)
      {
         // TargetType=Window styles do not apply to subclasses.
         window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
         window.SetResourceReference(Control.ForegroundProperty, "ForegroundBrush");
         ComputeTabIndex(window);
      }

      public static void ComputeTabIndex(this Window window)
      {
         int tabIndex = 0;
         _computeTabIndex(window, ref tabIndex);
      }

      private static void _computeTabIndex(DependencyObject depObj, ref int tabIndex)
      {
         if (depObj == null)
         {
            return;
         }

         for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
         {
            DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);

            if (ithChild is Control control)
            {
               control.TabIndex = tabIndex++;
            }

            _computeTabIndex(ithChild, ref tabIndex);
         }
      }

      /// <summary>
      /// Closes this dialog because the vault session ended. No-ops if the window
      /// is already shutting down (setting <see cref="Window.DialogResult"/> then throws).
      /// </summary>
      public static void DatabaseClosed(this Window window, bool IsClosing)
      {
         if (IsClosing || window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
         {
            return;
         }

         _ = window.Dispatcher.BeginInvoke(() =>
         {
            if (IsClosing || !window.IsLoaded)
            {
               return;
            }

            window.DialogResult = true;
         });
      }
   }
}
