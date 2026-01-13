using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
   }
}
