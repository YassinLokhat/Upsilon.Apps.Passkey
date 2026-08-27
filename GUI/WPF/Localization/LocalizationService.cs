using System.Globalization;
using System.Windows;

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
      /// Picks the OS UI language when we ship it; otherwise English.
      /// </summary>
      public static string ResolveDefaultLanguageCode()
      {
         string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
         return Supported.Any(l => string.Equals(l.Code, twoLetter, StringComparison.OrdinalIgnoreCase))
            ? twoLetter
            : DefaultLanguageCode;
      }

      /// <summary>
      /// Sets thread / default UI culture. When the culture actually changes, refreshes
      /// <see cref="TranslationSource"/> bindings and notifies open <see cref="ILanguageAware"/> surfaces.
      /// </summary>
      /// <returns><see langword="true"/> when the culture code changed.</returns>
      public static bool Apply(string? languageCode)
      {
         AppLanguage language = GetLanguageOrDefault(languageCode);
         string previous = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
         bool changed = !string.Equals(previous, language.Code, StringComparison.OrdinalIgnoreCase);

         CultureInfo culture = CultureInfo.GetCultureInfo(language.Code);

         CultureInfo.DefaultThreadCurrentCulture = culture;
         CultureInfo.DefaultThreadCurrentUICulture = culture;
         CultureInfo.CurrentCulture = culture;
         CultureInfo.CurrentUICulture = culture;
         Thread.CurrentThread.CurrentCulture = culture;
         Thread.CurrentThread.CurrentUICulture = culture;

         if (!changed)
         {
            return false;
         }

         TranslationSource.Instance.NotifyLanguageChanged();
         _notifyOpenWindows();
         LanguageChanged?.Invoke(null, EventArgs.Empty);
         return true;
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
