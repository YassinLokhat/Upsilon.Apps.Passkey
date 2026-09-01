using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;
using Upsilon.Apps.Passkey.GUI.MAUI.Localization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Themes
{
   internal sealed record AppThemeOption(string Code, string DisplayName);

   internal static class ThemeService
   {
      public const string SystemCode = "System";
      public const string LightCode = "Light";
      public const string DarkCode = "Dark";

      private static string _current = SystemCode;

      public static event EventHandler? ThemeChanged;

      public static string Current => _current;

      public static IReadOnlyList<AppThemeOption> Supported =>
      [
         new(SystemCode, Strings.EnumValue_Theme_System),
         new(LightCode, Strings.EnumValue_Theme_Light),
         new(DarkCode, Strings.EnumValue_Theme_Dark),
      ];

      public static AppThemeOption GetOptionOrDefault(string? code)
      {
         AppThemeOption? match = Supported.FirstOrDefault(t =>
            string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
         return match ?? Supported[0];
      }

      public static bool Apply(string themeCode, bool forceRefresh = false)
      {
         AppTheme requested = _resolve(themeCode);

         if (!forceRefresh
             && Application.Current?.UserAppTheme == requested
             && string.Equals(_current, themeCode, StringComparison.OrdinalIgnoreCase))
         {
            return false;
         }

         if (Application.Current is not null)
         {
            Application.Current.UserAppTheme = requested;
         }

         _current = themeCode;
         ThemeChanged?.Invoke(null, EventArgs.Empty);
         Log.Info($"Theme applied: {themeCode} → {requested}");
         return true;
      }

      public static bool ApplyEffective(string appTheme, string? userTheme)
      {
         string code = string.IsNullOrWhiteSpace(userTheme) ? appTheme : userTheme;
         return Apply(code);
      }

      private static AppTheme _resolve(string themeCode)
      {
         if (string.Equals(themeCode, LightCode, StringComparison.OrdinalIgnoreCase))
         {
            return AppTheme.Light;
         }

         if (string.Equals(themeCode, DarkCode, StringComparison.OrdinalIgnoreCase))
         {
            return AppTheme.Dark;
         }

         // System / Unspecified → follow OS
         return AppTheme.Unspecified;
      }
   }
}
