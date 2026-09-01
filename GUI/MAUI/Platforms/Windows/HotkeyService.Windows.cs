using System.Runtime.InteropServices;
using Upsilon.Apps.Passkey.GUI.MAUI.Services;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   internal static partial class HotkeyService
   {
      private const uint MOD_CONTROL = 0x0002;
      private const uint MOD_SHIFT = 0x0004;
      private const uint VK_L = 0x4C;
      private const uint VK_P = 0x50;
      private const int HOTKEY_ID_LOGIN = 0x5001;
      private const int HOTKEY_ID_PASSWORD = 0x5002;

      private static Func<string?>? _getIdentifier;
      private static Func<string?>? _getPassword;
      private static nint _hwnd;
      private static bool _registered;
      private static bool _wasDownL;
      private static bool _wasDownP;

      public static void Register(Func<string?> getIdentifier, Func<string?> getPassword)
      {
         Unregister();
         _getIdentifier = getIdentifier;
         _getPassword = getPassword;

         _hwnd = _tryGetHwnd();
         if (_hwnd != 0)
         {
            _ = RegisterHotKey(_hwnd, HOTKEY_ID_LOGIN, MOD_CONTROL | MOD_SHIFT, VK_L);
            _ = RegisterHotKey(_hwnd, HOTKEY_ID_PASSWORD, MOD_CONTROL | MOD_SHIFT, VK_P);
         }

         _registered = true;
         Application.Current?.Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), _pollHotkeys);
         Log.Info("HotkeyService registered Ctrl+Shift+L/P.");
      }

      public static void Unregister()
      {
         if (_hwnd != 0)
         {
            _ = UnregisterHotKey(_hwnd, HOTKEY_ID_LOGIN);
            _ = UnregisterHotKey(_hwnd, HOTKEY_ID_PASSWORD);
         }

         _registered = false;
         _hwnd = 0;
         _getIdentifier = null;
         _getPassword = null;
         _wasDownL = false;
         _wasDownP = false;
      }

      private static bool _pollHotkeys()
      {
         if (!_registered)
         {
            return false;
         }

         bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
         bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;
         bool l = (GetAsyncKeyState((int)VK_L) & 0x8000) != 0;
         bool p = (GetAsyncKeyState((int)VK_P) & 0x8000) != 0;

         if (ctrl && shift && l && !_wasDownL)
         {
            _copyAndPaste(_getIdentifier?.Invoke());
         }

         if (ctrl && shift && p && !_wasDownP)
         {
            _copyAndPaste(_getPassword?.Invoke());
         }

         _wasDownL = l;
         _wasDownP = p;
         return true;
      }

      private static void _copyAndPaste(string? text)
      {
         if (string.IsNullOrEmpty(text))
         {
            return;
         }

         AppServices.Clipboard.SetText(text, ClipboardManager.AutoClearAfter);
         keybd_event(0x11, 0, 0, 0);
         keybd_event(0x56, 0, 0, 0);
         keybd_event(0x56, 0, 2, 0);
         keybd_event(0x11, 0, 2, 0);
      }

      private static nint _tryGetHwnd()
      {
         try
         {
            IReadOnlyList<Window> windows = Application.Current?.Windows ?? [];
            if (windows.Count == 0)
            {
               return 0;
            }

            object? native = windows[0].Handler?.PlatformView;
            System.Reflection.PropertyInfo? prop = native?.GetType().GetProperty("WindowHandle");
            if (prop?.GetValue(native) is nint handle)
            {
               return handle;
            }
         }
         catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
         {
            Log.Warn($"HotkeyService hwnd: {ex.Message}");
         }

         return 0;
      }

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern bool UnregisterHotKey(nint hWnd, int id);

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern short GetAsyncKeyState(int vKey);

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);
   }
}
