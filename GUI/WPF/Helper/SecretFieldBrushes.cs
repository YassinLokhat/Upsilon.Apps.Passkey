using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Shared background for secret fields (account passwords and onion passkeys).
   /// Priority: dirty edit &gt; notified leak danger &gt; default unchanged.
   /// </summary>
   internal static class SecretFieldBrushes
   {
      public static Brush Background(bool isDirty, bool isNotifiedLeak)
         => isDirty
            ? DarkMode.ChangedBrush
            : isNotifiedLeak
               ? SemanticBrushes.Danger
               : DarkMode.UnchangedBrush2;
   }
}
