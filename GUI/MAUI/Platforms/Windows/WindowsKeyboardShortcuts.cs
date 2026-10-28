#if WINDOWS
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using KeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Platforms.Windows
{
   /// <summary>Page-scoped keyboard shortcuts without a visible menu bar.</summary>
   internal static class WindowsKeyboardShortcuts
   {
      /// <summary>VK_OEM_COMMA, absent from <see cref="VirtualKey"/>.</summary>
      internal const VirtualKey Comma = (VirtualKey)188;

      public static void Attach(VisualElement page, params (VirtualKeyModifiers Modifiers, VirtualKey Key, Action Action)[] shortcuts)
      {
         ArgumentNullException.ThrowIfNull(page);
         ArgumentNullException.ThrowIfNull(shortcuts);

         // OnHandlerChanged can fire more than once for the same native view.
         if (page.Handler?.PlatformView is not UIElement root || root.KeyboardAccelerators.Count > 0)
         {
            return;
         }

         root.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

         foreach ((VirtualKeyModifiers modifiers, VirtualKey key, Action action) in shortcuts)
         {
            KeyboardAccelerator accelerator = new() { Modifiers = modifiers, Key = key };
            accelerator.Invoked += (_, args) =>
            {
               args.Handled = true;
               action();
            };
            root.KeyboardAccelerators.Add(accelerator);
         }
      }

      public static Action Execute(ICommand command)
      {
         ArgumentNullException.ThrowIfNull(command);
         return () =>
         {
            if (command.CanExecute(null))
            {
               command.Execute(null);
            }
         };
      }
   }
}
#endif
