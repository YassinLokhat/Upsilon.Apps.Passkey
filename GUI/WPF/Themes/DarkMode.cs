using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   internal static class DarkMode
   {
      /// <summary>Background brush used for the main window/panel surfaces.</summary>
      public static Brush UnchangedBrush1 { get; private set; } = _freeze(Color.FromRgb(0x1E, 0x1E, 0x1E));

      /// <summary>Background brush used for inputs that have not been modified.</summary>
      public static Brush UnchangedBrush2 { get; private set; } = _freeze(Color.FromRgb(0x2D, 0x2D, 0x30));

      /// <summary>Background brush used to highlight inputs whose value has been modified.</summary>
      public static Brush ChangedBrush { get; private set; } = _freeze(Color.FromRgb(0x60, 0x60, 0x60));

      /// <summary>
      /// Re-reads item-state brushes from the active theme dictionary. Falls back
      /// to the current values when <see cref="Application.Current"/> is unset
      /// (unit tests).
      /// </summary>
      public static void SyncFromApplicationResources()
      {
         UnchangedBrush1 = _resourceOr(nameof(UnchangedBrush1), UnchangedBrush1);
         UnchangedBrush2 = _resourceOr(nameof(UnchangedBrush2), UnchangedBrush2);
         ChangedBrush = _resourceOr(nameof(ChangedBrush), ChangedBrush);
      }

      private static Brush _resourceOr(string key, Brush fallback)
         => Application.Current?.TryFindResource(key) is Brush brush ? brush : fallback;

      private static SolidColorBrush _freeze(Color color)
      {
         SolidColorBrush brush = new(color);
         brush.Freeze();
         return brush;
      }
   }
}
