using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   internal static class DarkMode
   {
      /// <summary>Background brush used for the main window/panel surfaces (#1E1E1E).</summary>
      public static readonly Brush UnchangedBrush1 = _freeze(Color.FromRgb(0x1E, 0x1E, 0x1E));

      /// <summary>Background brush used for inputs that have not been modified (#2D2D30).</summary>
      public static readonly Brush UnchangedBrush2 = _freeze(Color.FromRgb(0x2D, 0x2D, 0x30));

      /// <summary>Background brush used to highlight inputs whose value has been modified (#606060).</summary>
      public static readonly Brush ChangedBrush = _freeze(Color.FromRgb(0x60, 0x60, 0x60));

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

      private static SolidColorBrush _freeze(Color color)
      {
         SolidColorBrush brush = new(color);
         brush.Freeze();
         return brush;
      }

      [DllImport("dwmapi.dll", PreserveSig = true)]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
   }
}
