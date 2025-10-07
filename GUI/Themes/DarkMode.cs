using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Upsilon.Apps.Passkey.GUI.Themes
{
   public static class DarkMode
   {
      public static void SetDarkMode(Window window)
      {
         _setDarkTitleBar(window);

         var dictionaries = Application.Current.Resources.MergedDictionaries;
         dictionaries.Clear();

         var themeDict = new ResourceDictionary
         {
            Source = new Uri($"Themes/DarkTheme.xaml", UriKind.Relative)
         };

         dictionaries.Add(themeDict);
      }

      private static void _setDarkTitleBar(Window window)
      {
         var hwnd = new WindowInteropHelper(window).Handle;

         if (hwnd == IntPtr.Zero)
            return;

         int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
         int useImmersiveDarkMode = 1;
         DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int));
      }

      [DllImport("dwmapi.dll", PreserveSig = true)]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
   }
}
