using System.Runtime.InteropServices;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// The few Win32 calls the custom chrome needs (dwmapi, part of Windows).
   /// </summary>
   internal static class NativeMethods
   {
      /// <summary>DWMWA_WINDOW_CORNER_PREFERENCE (Windows 11).</summary>
      internal const int DwmWindowCornerPreference = 33;

      /// <summary>DWMWCP_DONOTROUND: keep square corners.</summary>
      internal const int DwmCornerDoNotRound = 1;

      /// <summary>DWMWA_CLOAK: hide a window from the screen while it stays shown to the app.</summary>
      internal const int DwmCloak = 13;

      internal const int GwlExStyle = -20;

      /// <summary>WS_EX_TRANSPARENT: mouse input passes through the window.</summary>
      internal const int WsExTransparent = 0x20;

      /// <summary>WS_EX_NOACTIVATE: the window never takes focus.</summary>
      internal const int WsExNoActivate = 0x08000000;

      [DllImport("dwmapi.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

      [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern int GetWindowLong(IntPtr hwnd, int index);

      [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern int SetWindowLong(IntPtr hwnd, int index, int value);

      /// <summary>Cloaks (or uncloaks) a window; false when the compositor refused.</summary>
      internal static bool Cloak(IntPtr hwnd, bool cloak)
      {
         int value = cloak ? 1 : 0;
         return DwmSetWindowAttribute(hwnd, DwmCloak, ref value, sizeof(int)) == 0;
      }

      /// <summary>Makes a window click-through and never activated.</summary>
      internal static void MakeClickThrough(IntPtr hwnd)
      {
         _ = SetWindowLong(hwnd, GwlExStyle, GetWindowLong(hwnd, GwlExStyle) | WsExTransparent | WsExNoActivate);
      }
   }
}
