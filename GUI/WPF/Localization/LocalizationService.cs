using System.Globalization;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Applies the UI culture and lists languages shipping with the WPF client.
   /// To add a language: copy <c>Strings.resx</c> → <c>Strings.xx.resx</c>, translate,
   /// then append one entry to <see cref="Supported"/>.
   /// Translators must keep both <c>EnumValue_ActivityEventType_*</c> (short labels)
   /// and <c>Activity_*</c> (full Message sentences) in sync — see Wiki WPF Client Localization.
   /// </summary>
   internal static class LocalizationService
   {
      public const string DefaultLanguageCode = "en";

      /// <summary>
      /// Languages with a satellite resource assembly. Order is display order in App Settings.
      /// </summary>
      public static IReadOnlyList<AppLanguage> Supported { get; } =
      [
         new(DefaultLanguageCode, "English"),
         new("fr", "Français"),
      ];

      public static AppLanguage GetLanguageOrDefault(string? code)
      {
         if (!string.IsNullOrWhiteSpace(code))
         {
            AppLanguage? match = Supported.FirstOrDefault(l =>
               string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
               return match;
            }
         }

         return Supported[0];
      }

      /// <summary>
      /// Picks the OS UI language when we ship it; otherwise English.
      /// </summary>
      public static string ResolveDefaultLanguageCode()
      {
         string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
         return Supported.Any(l => string.Equals(l.Code, twoLetter, StringComparison.OrdinalIgnoreCase))
            ? twoLetter
            : DefaultLanguageCode;
      }

      public static void Apply(string? languageCode)
      {
         AppLanguage language = GetLanguageOrDefault(languageCode);
         CultureInfo culture = CultureInfo.GetCultureInfo(language.Code);

         CultureInfo.DefaultThreadCurrentCulture = culture;
         CultureInfo.DefaultThreadCurrentUICulture = culture;
         CultureInfo.CurrentCulture = culture;
         CultureInfo.CurrentUICulture = culture;
         Thread.CurrentThread.CurrentCulture = culture;
         Thread.CurrentThread.CurrentUICulture = culture;
      }
   }
}
