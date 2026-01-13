using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   public static class DarkMode
   {
      public static Brush UnchangedBrush1 => new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
      public static Brush UnchangedBrush2 => new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
      public static Brush ChangedBrush => new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));

      public static void SetDarkMode(Window window)
      {
         nint hwnd = new WindowInteropHelper(window).Handle;

         if (hwnd == IntPtr.Zero)
         {
            return;
         }

         int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
         int useImmersiveDarkMode = 1;
         _ = DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int));
      }

      [DllImport("dwmapi.dll", PreserveSig = true)]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
   }
}
