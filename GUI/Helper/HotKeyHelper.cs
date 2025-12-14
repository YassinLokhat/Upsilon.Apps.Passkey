using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Upsilon.Apps.Passkey.GUI.Helper
{
   public static class HotkeyHelper
   {
      private static int _id = 0;

      public static event EventHandler<HotkeyEventArgs>? HotkeyPressed;

      public static int Register(Window window, ModifierKeys modifiers, Key key)
      {
         int hotkeyId = Interlocked.Increment(ref _id);
         uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);

         IntPtr hWnd = new WindowInteropHelper(window).Handle;
         if (hWnd == IntPtr.Zero)
            return -1;

         bool success = RegisterHotKey(hWnd, hotkeyId, (uint)modifiers, virtualKey);
         if (!success)
            return -1;

         if (PresentationSource.FromVisual(window) is HwndSource source)
         {
            source.AddHook((hwnd, msg, wParam, lParam, ref handled) =>
            {
               if (msg == 0x0312 && (int)wParam == hotkeyId)
               {
                  HotkeyEventArgs e = new(lParam);
                  HotkeyPressed?.Invoke(window, e);
                  handled = true;
               }
               return IntPtr.Zero;
            });
         }

         return hotkeyId;
      }

      public static bool Unregister(Window window, int hotkeyId)
      {
         if (window is null)
            return false;

         IntPtr hWnd = new WindowInteropHelper(window).Handle;
         return hWnd != nint.Zero && UnregisterHotKey(hWnd, hotkeyId);
      }

      public static void Send(ModifierKeys modifiers, Key key)
      {
         //byte[] modifiers = [];
         byte virtualKey = (byte)KeyInterop.VirtualKeyFromKey(key);

         keybd_event((byte)modifiers, 0, 0, UIntPtr.Zero);

         keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
         keybd_event(virtualKey, 0, 0x0002, UIntPtr.Zero);

         keybd_event((byte)modifiers, 0, 0x0002, UIntPtr.Zero);
      }

      [DllImport("user32.dll")]
      private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
      private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      [DllImport("user32.dll")]
      private static extern uint MapVirtualKey(uint uCode, uint uMapType);

      [DllImport("user32.dll")]
      private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
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