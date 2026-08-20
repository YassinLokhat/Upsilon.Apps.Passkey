using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Global hotkeys via <c>RegisterHotKey</c>, plus <c>SendInput</c> to synthesize
   /// Ctrl+V after copying. Returns -1 when registration fails (handle not ready,
   /// or the combo is already taken).
   /// </summary>
   internal static class HotkeyHelper
   {
      private const int WM_HOTKEY = 0x0312;
      private const int PASTE_DELAY_MS = 100;
      private static int _id;

      private static readonly Dictionary<int, Registration> _registrations = [];

      public static event EventHandler<HotkeyEventArgs>? HotkeyPressed;

      public static int Register(Window window, ModifierKeys modifiers, Key key)
      {
         int hotkeyId = Interlocked.Increment(ref _id);
         uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);

         IntPtr hWnd = new WindowInteropHelper(window).Handle;
         if (hWnd == IntPtr.Zero)
         {
            return -1;
         }

         if (!RegisterHotKey(hWnd, hotkeyId, (uint)modifiers, virtualKey))
         {
            Log.Warn($"RegisterHotKey failed for {modifiers}+{key}.");
            return -1;
         }

         if (PresentationSource.FromVisual(window) is not HwndSource source)
         {
            _ = UnregisterHotKey(hWnd, hotkeyId);
            return -1;
         }

         IntPtr expected = hotkeyId;
         nint hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
         {
            if (msg == WM_HOTKEY && wParam == expected)
            {
               HotkeyPressed?.Invoke(window, new HotkeyEventArgs(lParam));
               handled = true;
            }

            return IntPtr.Zero;
         }

         source.AddHook(hook);
         _registrations[hotkeyId] = new Registration(hWnd, source, hook);

         return hotkeyId;
      }

      public static bool Unregister(Window window, int hotkeyId)
      {
         if (window is null
            || !_registrations.Remove(hotkeyId, out Registration? registration))
         {
            return false;
         }

         registration.Source.RemoveHook(registration.Hook);
         return registration.Handle != IntPtr.Zero && UnregisterHotKey(registration.Handle, hotkeyId);
      }

      /// <summary>
      /// Synthesises Ctrl+V in the active window after a short delay so hotkey
      /// modifiers (Ctrl+Shift) are released before injection; immediate SendKeys
      /// often drops the Ctrl prefix and types plain "v".
      /// </summary>
      public static void SendPaste()
      {
         DispatcherTimer timer = new(DispatcherPriority.Normal, Application.Current.Dispatcher)
         {
            Interval = TimeSpan.FromMilliseconds(PASTE_DELAY_MS),
         };
         timer.Tick += (_, _) =>
         {
            timer.Stop();
            _sendCtrlV();
         };
         timer.Start();
      }

      private static void _sendCtrlV()
      {
         INPUT[] inputs =
         [
            _keyInput(0x11, keyUp: false), // VK_CONTROL down
            _keyInput(0x56, keyUp: false), // VK_V down
            _keyInput(0x56, keyUp: true),  // VK_V up
            _keyInput(0x11, keyUp: true),  // VK_CONTROL up
         ];

         _ = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
      }

      private static INPUT _keyInput(ushort virtualKey, bool keyUp)
      {
         return new INPUT
         {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
               ki = new KEYBDINPUT
               {
                  wVk = virtualKey,
                  wScan = 0,
                  dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                  time = 0,
                  dwExtraInfo = IntPtr.Zero,
               },
            },
         };
      }

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      [DllImport("user32.dll", SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

      private const uint INPUT_KEYBOARD = 1;
      private const uint KEYEVENTF_KEYUP = 0x0002;

      [StructLayout(LayoutKind.Sequential)]
      private struct INPUT
      {
         public uint type;
         public InputUnion U;
      }

      [StructLayout(LayoutKind.Explicit)]
      private struct InputUnion
      {
         [FieldOffset(0)]
         public KEYBDINPUT ki;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct KEYBDINPUT
      {
         public ushort wVk;
         public ushort wScan;
         public uint dwFlags;
         public uint time;
         public IntPtr dwExtraInfo;
      }

      private sealed record Registration(IntPtr Handle, HwndSource Source, HwndSourceHook Hook);
   }

   internal sealed class HotkeyEventArgs : EventArgs
   {
      public readonly Key Key;
      public readonly ModifierKeys Modifiers;

      internal HotkeyEventArgs(IntPtr hotKeyParam)
      {
         uint param = (uint)hotKeyParam.ToInt64();
         int virtualKey = (int)((param & 0xffff0000) >> 16);
         Key = KeyInterop.KeyFromVirtualKey(virtualKey);
         Modifiers = (ModifierKeys)(param & 0x0000ffff);
      }
   }
}
