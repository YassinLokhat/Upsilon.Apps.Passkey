namespace Upsilon.Apps.Passkey.GUI.Presentation.Models
{
   /// <summary>
   /// Persisted theme preference codes (app settings, user settings, activity
   /// <c>FieldValue</c>). Hosts map them to their own resources.
   /// </summary>
   internal static class ThemePreferences
   {
      public const string SystemCode = "System";
      public const string LightCode = "Light";
      public const string DarkCode = "Dark";

      /// <summary>
      /// User override when it is a known code; otherwise the application
      /// preference, falling back to <see cref="SystemCode"/>.
      /// </summary>
      public static string ResolveEffective(string? appTheme, string? userThemeOverride)
         => _known(userThemeOverride) ?? _known(appTheme) ?? SystemCode;

      private static string? _known(string? code)
         => new[] { SystemCode, LightCode, DarkCode }
            .FirstOrDefault(x => string.Equals(x, code?.Trim(), StringComparison.OrdinalIgnoreCase));
   }
}
