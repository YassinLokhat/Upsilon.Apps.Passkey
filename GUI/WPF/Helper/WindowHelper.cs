using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   public static class WindowHelper
   {
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

      public static void PostLoadSetup(this Window window)
      {
         DarkMode.SetDarkMode(window);
         ComputeTabIndex(window);
      }

      public static void ComputeTabIndex(this Window window)
      {
         int tabIndex = 0;
         _computeTabIndex(window, ref tabIndex);
      }

      private static void _computeTabIndex(DependencyObject depObj, ref int tabIndex)
      {
         if (depObj == null) return;

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
   }
}
