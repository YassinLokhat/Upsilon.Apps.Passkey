using Upsilon.Apps.Passkey.GUI.Presentation.Localization;

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

      /// <summary>Theme choices in settings display order.</summary>
      public static IReadOnlyList<AppThemeOption> Options =>
      [
         new(SystemCode, Strings.EnumValue_Theme_System),
         new(LightCode, Strings.EnumValue_Theme_Light),
         new(DarkCode, Strings.EnumValue_Theme_Dark),
      ];

      public static AppThemeOption GetOptionOrDefault(string? code)
         => Options.FirstOrDefault(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Options[0];

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
