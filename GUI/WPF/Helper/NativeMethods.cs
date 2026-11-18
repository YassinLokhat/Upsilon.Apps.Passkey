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

      [DllImport("dwmapi.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
   }
}
