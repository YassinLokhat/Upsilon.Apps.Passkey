using System.Windows;
using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// Centralised set of frozen brushes used to convey state semantics throughout
   /// the UI. The same colors are exposed as resources in the active theme
   /// dictionary; <see cref="SyncFromApplicationResources"/> keeps both in sync.
   /// </summary>
   internal static class SemanticBrushes
   {
      public static Brush Info { get; private set; } = _freeze(Color.FromRgb(0xFF, 0xFF, 0xFF));
      public static Brush Success { get; private set; } = _freeze(Color.FromRgb(0x52, 0xC1, 0x6E));
      public static Brush Warning { get; private set; } = _freeze(Color.FromRgb(0xFF, 0xD8, 0x4A));
      public static Brush Danger { get; private set; } = _freeze(Color.FromRgb(0xE0, 0x4A, 0x4A));
      public static Brush DisabledForeground { get; private set; } = _freeze(Color.FromRgb(0x80, 0x80, 0x80));

      public static void SyncFromApplicationResources()
      {
         Info = _resourceOr("InfoBrush", Info);
         Success = _resourceOr("SuccessBrush", Success);
         Warning = _resourceOr("WarningBrush", Warning);
         Danger = _resourceOr("DangerBrush", Danger);
         DisabledForeground = _resourceOr("DisabledForegroundBrush", DisabledForeground);
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
