using System.Globalization;
using Upsilon.Apps.Passkey.GUI.MAUI.Helpers;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   internal static class LocalizationService
   {
      public const string SystemCode = "System";

      public static readonly (string Code, string NativeName)[] Shipped =
      [
         (SystemCode, "System"),
         ("en", "English"),
         ("fr", "Français"),
      ];

      private static string _currentCode = SystemCode;

      public static event EventHandler? LanguageChanged;

      public static string CurrentCode => _currentCode;

      public static bool Apply(string languageCode, bool forceRefresh = false)
      {
         string resolved = _resolve(languageCode);

         CultureInfo culture = resolved switch
         {
            "fr" => new CultureInfo("fr"),
            _ => new CultureInfo("en"),
         };

         if (!forceRefresh
             && CultureInfo.CurrentUICulture.Name.StartsWith(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
             && string.Equals(_currentCode, languageCode, StringComparison.OrdinalIgnoreCase))
         {
            return false;
         }

         CultureInfo.CurrentCulture = culture;
         CultureInfo.CurrentUICulture = culture;
         CultureInfo.DefaultThreadCurrentCulture = culture;
         CultureInfo.DefaultThreadCurrentUICulture = culture;
         Strings.Culture = culture;
         _currentCode = languageCode;
         LanguageChanged?.Invoke(null, EventArgs.Empty);
         Log.Info($"Language applied: {languageCode} → {culture.Name}");
         return true;
      }

      public static bool ApplyEffective(string appLanguage, string? userLanguage)
      {
         string code = string.IsNullOrWhiteSpace(userLanguage) ? appLanguage : userLanguage;
         return Apply(code);
      }

      private static string _resolve(string languageCode)
      {
         if (string.IsNullOrWhiteSpace(languageCode) || string.Equals(languageCode, SystemCode, StringComparison.OrdinalIgnoreCase))
         {
            string os = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Shipped.Any(s => s.Code == os) ? os : "en";
         }

         return languageCode;
      }
   }
}
