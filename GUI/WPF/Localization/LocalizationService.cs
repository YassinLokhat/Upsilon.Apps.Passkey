using System.Globalization;
using System.Windows;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Applies the UI culture and lists languages shipping with the WPF client.
   /// To add a language: copy <c>Strings.resx</c> → <c>Strings.xx.resx</c>, translate,
   /// then append one entry to <see cref="Shipped"/>.
   /// Translators must keep both <c>EnumValue_ActivityEventType_*</c> (short labels)
   /// and <c>Activity_*</c> (full Message sentences) in sync — see Wiki WPF Client Localization.
   /// </summary>
   internal static class LocalizationService
   {
      public const string SystemCode = "System";
      public const string DefaultLanguageCode = "en";

      /// <summary>
      /// Cultures with a satellite resource assembly. Order is display order in
      /// App Settings after <see cref="SystemCode"/>.
      /// </summary>
      public static IReadOnlyList<AppLanguage> Shipped { get; } =
      [
         new(DefaultLanguageCode, "English"),
         new("fr", "Français"),
      ];

      /// <summary>
      /// Language preferences the client knows how to apply. Order is display
      /// order in App Settings. <see cref="SystemCode"/> follows the OS UI
      /// language when we ship it; otherwise English.
      /// </summary>
      public static IReadOnlyList<AppLanguage> Supported =>
      [
         new(SystemCode, Strings.EnumValue_Theme_System),
         .. Shipped,
      ];

      private static readonly string _osUiLanguageAtLoad =
         CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

      /// <summary>
      /// Test seam for "same as system". Production captures
      /// <see cref="CultureInfo.CurrentUICulture"/> at process load, before
      /// <see cref="Apply"/> overwrites it.
      /// </summary>
      internal static Func<string> DetectSystemLanguageCode { get; set; } = static () => _osUiLanguageAtLoad;

      /// <summary>
      /// Raised on the UI thread after <see cref="Apply"/> changes the culture.
      /// Prefer implementing <see cref="ILanguageAware"/> on open windows / DataContexts;
      /// this event is for tests and rare non-visual listeners.
      /// </summary>
      public static event EventHandler? LanguageChanged;

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
      /// User override when non-empty and a known preference; otherwise the
      /// application preference. Result is still a preference
      /// (<c>System</c>/<c>en</c>/<c>fr</c>, …), not a resolved culture.
      /// </summary>
      public static string ResolveEffectiveLanguageCode(string? appLanguage, string? userLanguageOverride)
      {
         if (!string.IsNullOrWhiteSpace(userLanguageOverride))
         {
            AppLanguage? match = Supported.FirstOrDefault(l =>
               string.Equals(l.Code, userLanguageOverride, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
               return match.Code;
            }
         }

         return GetLanguageOrDefault(appLanguage).Code;
      }

      /// <summary>
      /// Maps a preference to a shipped culture code.
      /// </summary>
      public static string ResolveCultureCode(string? preference)
      {
         string code = GetLanguageOrDefault(preference).Code;
         if (!string.Equals(code, SystemCode, StringComparison.OrdinalIgnoreCase))
         {
            return code;
         }

         string os = DetectSystemLanguageCode();
         AppLanguage? match = Shipped.FirstOrDefault(l =>
            string.Equals(l.Code, os, StringComparison.OrdinalIgnoreCase));
         return match?.Code ?? DefaultLanguageCode;
      }

      /// <summary>
      /// Applies <see cref="ResolveEffectiveLanguageCode"/> then the resolved culture.
      /// </summary>
      public static bool ApplyEffective(string? appLanguage, string? userLanguageOverride)
         => Apply(ResolveEffectiveLanguageCode(appLanguage, userLanguageOverride));

      /// <summary>
      /// Picks the OS UI language when we ship it; otherwise English.
      /// </summary>
      public static string ResolveDefaultLanguageCode()
         => ResolveCultureCode(SystemCode);

      /// <summary>
      /// Sets thread / default UI culture. When the culture actually changes (or
      /// <paramref name="forceRefresh"/> is set), refreshes <see cref="TranslationSource"/>
      /// bindings and notifies open <see cref="ILanguageAware"/> surfaces.
      /// </summary>
      /// <returns><see langword="true"/> when the culture code changed.</returns>
      public static bool Apply(string? languageCode, bool forceRefresh = false)
      {
         string cultureCode = ResolveCultureCode(languageCode);
         string previous = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
         bool changed = !string.Equals(previous, cultureCode, StringComparison.OrdinalIgnoreCase);

         CultureInfo culture = CultureInfo.GetCultureInfo(cultureCode);

         CultureInfo.DefaultThreadCurrentCulture = culture;
         CultureInfo.DefaultThreadCurrentUICulture = culture;
         CultureInfo.CurrentCulture = culture;
         CultureInfo.CurrentUICulture = culture;
         Thread.CurrentThread.CurrentCulture = culture;
         Thread.CurrentThread.CurrentUICulture = culture;

         if (!changed && !forceRefresh)
         {
            return false;
         }

         TranslationSource.Instance.NotifyLanguageChanged();
         _notifyOpenWindows();
         LanguageChanged?.Invoke(null, EventArgs.Empty);
         return changed;
      }

      private static void _notifyOpenWindows()
      {
         Application? app = Application.Current;
         if (app is null)
         {
            return;
         }

         void notify()
         {
            foreach (Window window in app.Windows.Cast<Window>().ToArray())
            {
               if (window is ILanguageAware windowAware)
               {
                  windowAware.OnLanguageChanged();
               }

               if (window.DataContext is ILanguageAware dataContextAware)
               {
                  dataContextAware.OnLanguageChanged();
               }
            }
         }

         if (app.Dispatcher.CheckAccess())
         {
            notify();
         }
         else
         {
            app.Dispatcher.Invoke(notify);
         }
      }
   }
}
