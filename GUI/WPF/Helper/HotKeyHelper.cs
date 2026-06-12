using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   public static class HotkeyHelper
   {
      private const int WM_HOTKEY = 0x0312;
      private static int _id = 0;

      private static readonly Dictionary<int, _Registration> _registrations = [];

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

         IntPtr expected = (IntPtr)hotkeyId;
         HwndSourceHook hook = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
         {
            if (msg == WM_HOTKEY && wParam == expected)
            {
               HotkeyPressed?.Invoke(window, new HotkeyEventArgs(lParam));
               handled = true;
            }

            return IntPtr.Zero;
         };

         source.AddHook(hook);
         _registrations[hotkeyId] = new _Registration(hWnd, source, hook);

         return hotkeyId;
      }

      public static bool Unregister(Window window, int hotkeyId)
      {
         if (window is null
            || !_registrations.Remove(hotkeyId, out _Registration? registration))
         {
            return false;
         }

         registration.Source.RemoveHook(registration.Hook);
         return registration.Handle != IntPtr.Zero && UnregisterHotKey(registration.Handle, hotkeyId);
      }

      /// <summary>
      /// Synthesises a keystroke (modifiers + key) using <c>SendInput</c>, which
      /// supersedes the legacy <c>keybd_event</c>.
      /// </summary>
      public static void Send(ModifierKeys modifiers, Key key)
      {
         List<INPUT> inputs = [];

         _appendModifierInputs(inputs, modifiers, keyUp: false);
         _appendKeyInput(inputs, (ushort)KeyInterop.VirtualKeyFromKey(key), keyUp: false);
         _appendKeyInput(inputs, (ushort)KeyInterop.VirtualKeyFromKey(key), keyUp: true);
         _appendModifierInputs(inputs, modifiers, keyUp: true);

         INPUT[] array = [.. inputs];
         _ = SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
      }

      private static void _appendModifierInputs(List<INPUT> inputs, ModifierKeys modifiers, bool keyUp)
      {
         if (modifiers.HasFlag(ModifierKeys.Control))
         {
            _appendKeyInput(inputs, 0x11, keyUp); // VK_CONTROL
         }

         if (modifiers.HasFlag(ModifierKeys.Shift))
         {
            _appendKeyInput(inputs, 0x10, keyUp); // VK_SHIFT
         }

         if (modifiers.HasFlag(ModifierKeys.Alt))
         {
            _appendKeyInput(inputs, 0x12, keyUp); // VK_MENU
         }

         if (modifiers.HasFlag(ModifierKeys.Windows))
         {
            _appendKeyInput(inputs, 0x5B, keyUp); // VK_LWIN
         }
      }

      private static void _appendKeyInput(List<INPUT> inputs, ushort virtualKey, bool keyUp)
      {
         inputs.Add(new INPUT
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
         });
      }

      [DllImport("user32.dll")]
      private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
      private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      [DllImport("user32.dll", SetLastError = true)]
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
         public MOUSEINPUT mi;
         [FieldOffset(0)]
         public KEYBDINPUT ki;
         [FieldOffset(0)]
         public HARDWAREINPUT hi;
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

      [StructLayout(LayoutKind.Sequential)]
      private struct MOUSEINPUT
      {
         public int dx;
         public int dy;
         public uint mouseData;
         public uint dwFlags;
         public uint time;
         public IntPtr dwExtraInfo;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct HARDWAREINPUT
      {
         public uint uMsg;
         public ushort wParamL;
         public ushort wParamH;
      }

      private sealed record _Registration(IntPtr Handle, HwndSource Source, HwndSourceHook Hook);
   }

   public class HotkeyEventArgs : EventArgs
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
