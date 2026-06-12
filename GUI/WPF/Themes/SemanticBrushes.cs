using System.Windows.Media;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// Centralised set of frozen brushes used to convey state semantics throughout
   /// the UI. The same colors are exposed as <c>StaticResource</c>s in
   /// <c>DarkTheme.xaml</c>; keep both in sync.
   /// </summary>
   public static class SemanticBrushes
   {
      public static readonly Brush Info = _freeze(Color.FromRgb(0xFF, 0xFF, 0xFF));
      public static readonly Brush Success = _freeze(Color.FromRgb(0x52, 0xC1, 0x6E));
      public static readonly Brush Warning = _freeze(Color.FromRgb(0xFF, 0xD8, 0x4A));
      public static readonly Brush Danger = _freeze(Color.FromRgb(0xE0, 0x4A, 0x4A));
      public static readonly Brush DisabledForeground = _freeze(Color.FromRgb(0x80, 0x80, 0x80));

      private static Brush _freeze(Color color)
      {
         SolidColorBrush brush = new(color);
         brush.Freeze();
         return brush;
      }
   }
}
