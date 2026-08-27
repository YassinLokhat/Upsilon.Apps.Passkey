using System.IO;
using System.Security;
using System.Windows;
using Microsoft.Win32;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// Resolves the effective UI theme (app default, optional user override,
   /// System → OS light/dark) and swaps the color resource dictionary.
   /// </summary>
   internal static class ThemeService
   {
      public const string SystemCode = "System";
      public const string LightCode = "Light";
      public const string DarkCode = "Dark";

      /// <summary>
      /// Theme preferences the client knows how to apply. Order is display order
      /// in App Settings.
      /// </summary>
      public static IReadOnlyList<AppThemeOption> Supported =>
      [
         new(SystemCode, Strings.EnumValue_Theme_System),
         new(LightCode, Strings.EnumValue_Theme_Light),
         new(DarkCode, Strings.EnumValue_Theme_Dark),
      ];

      /// <summary>
      /// Last preference passed to <see cref="Apply"/> (<c>System</c>, <c>Light</c>,
      /// or <c>Dark</c>). Empty until the first apply.
      /// </summary>
      public static string CurrentPreference { get; private set; } = string.Empty;

      /// <summary>
      /// Resolved appearance actually painted (<c>Light</c> or <c>Dark</c>).
      /// Empty until the first apply.
      /// </summary>
      public static string CurrentAppearance { get; private set; } = string.Empty;

      public static bool IsDarkAppearance
         => string.Equals(CurrentAppearance, DarkCode, StringComparison.OrdinalIgnoreCase);

      /// <summary>
      /// Test seam for "same as system". Production reads
      /// <c>HKCU\...\Personalize\AppsUseLightTheme</c>.
      /// </summary>
      internal static Func<bool> DetectSystemLightTheme { get; set; } = _readWindowsAppsUseLightTheme;

      /// <summary>
      /// Raised on the UI thread after <see cref="Apply"/> changes the appearance
      /// (or <c>forceRefresh</c>). Prefer implementing <see cref="IThemeAware"/>
      /// on open windows / DataContexts; this event is for tests and rare listeners.
      /// </summary>
      public static event EventHandler? ThemeChanged;

      private static bool _listeningOs;

      public static AppThemeOption GetOptionOrDefault(string? code)
      {
         string resolved = GetThemeOrDefault(code);
         return Supported.First(t =>
            string.Equals(t.Code, resolved, StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>
      /// Known preference or <see cref="SystemCode"/>.
      /// </summary>
      public static string GetThemeOrDefault(string? code)
      {
         if (!string.IsNullOrWhiteSpace(code))
         {
            AppThemeOption? match = Supported.FirstOrDefault(t =>
               string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
               return match.Code;
            }
         }

         return SystemCode;
      }

      /// <summary>
      /// User override when non-empty and supported; otherwise the application theme.
      /// Result is still a preference (<c>System</c>/<c>Light</c>/<c>Dark</c>), not
      /// a resolved appearance.
      /// </summary>
      public static string ResolveEffectivePreference(string? appTheme, string? userThemeOverride)
      {
         if (!string.IsNullOrWhiteSpace(userThemeOverride))
         {
            AppThemeOption? match = Supported.FirstOrDefault(t =>
               string.Equals(t.Code, userThemeOverride, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
               return match.Code;
            }
         }

         return GetThemeOrDefault(appTheme);
      }

      /// <summary>
      /// Maps a preference to <see cref="LightCode"/> or <see cref="DarkCode"/>.
      /// </summary>
      public static string ResolveAppearance(string? preference)
      {
         string code = GetThemeOrDefault(preference);
         if (string.Equals(code, SystemCode, StringComparison.OrdinalIgnoreCase))
         {
            return DetectSystemLightTheme() ? LightCode : DarkCode;
         }

         return code;
      }

      /// <summary>
      /// Applies <see cref="ResolveEffectivePreference"/> then paints that appearance.
      /// </summary>
      public static bool ApplyEffective(string? appTheme, string? userThemeOverride)
         => Apply(ResolveEffectivePreference(appTheme, userThemeOverride));

      /// <summary>
      /// Swaps the color dictionary, syncs code-behind brushes, and updates
      /// immersive title bars. When the culture-independent appearance actually
      /// changes (or <paramref name="forceRefresh"/> is set), notifies open
      /// <see cref="IThemeAware"/> surfaces.
      /// </summary>
      /// <returns><see langword="true"/> when the painted appearance changed.</returns>
      public static bool Apply(string? preference, bool forceRefresh = false)
      {
         _ensureOsListener();

         string pref = GetThemeOrDefault(preference);
         string appearance = ResolveAppearance(pref);
         bool appearanceChanged = !string.Equals(CurrentAppearance, appearance, StringComparison.OrdinalIgnoreCase);
         bool preferenceChanged = !string.Equals(CurrentPreference, pref, StringComparison.OrdinalIgnoreCase);

         CurrentPreference = pref;
         CurrentAppearance = appearance;

         if (!appearanceChanged && !preferenceChanged && !forceRefresh)
         {
            return false;
         }

         _swapColorDictionary(appearance);
         DarkMode.SyncFromApplicationResources();
         SemanticBrushes.SyncFromApplicationResources();
         _applyTitleBars();
         _notifyOpenWindows();
         ThemeChanged?.Invoke(null, EventArgs.Empty);
         return appearanceChanged;
      }

      /// <summary>
      /// Drops the OS theme listener. Call from application exit.
      /// </summary>
      public static void Shutdown()
      {
         if (!_listeningOs)
         {
            return;
         }

         SystemEvents.UserPreferenceChanged -= _onUserPreferenceChanged;
         _listeningOs = false;
      }

      private static void _ensureOsListener()
      {
         // SystemEvents installs a native window; without a WPF Application (unit
         // tests) that crashes the test host on unload. Live OS follow only
         // matters when there are windows to repaint.
         if (_listeningOs || Application.Current is null)
         {
            return;
         }

         SystemEvents.UserPreferenceChanged += _onUserPreferenceChanged;
         _listeningOs = true;
      }

      private static void _onUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
      {
         if (e.Category != UserPreferenceCategory.General)
         {
            return;
         }

         if (!string.Equals(CurrentPreference, SystemCode, StringComparison.OrdinalIgnoreCase))
         {
            return;
         }

         _dispatch(() => Apply(CurrentPreference, forceRefresh: true));
      }

      private static void _swapColorDictionary(string appearance)
      {
         Application? app = Application.Current;
         if (app is null)
         {
            return;
         }

         string file = string.Equals(appearance, LightCode, StringComparison.OrdinalIgnoreCase)
            ? "LightTheme.xaml"
            : "DarkTheme.xaml";
         ResourceDictionary colors = new()
         {
            Source = new Uri($"pack://application:,,,/Themes/{file}", UriKind.Absolute),
         };

         void swap()
         {
            if (app.Resources.MergedDictionaries.Count == 0)
            {
               app.Resources.MergedDictionaries.Add(colors);
               return;
            }

            app.Resources.MergedDictionaries[0] = colors;
         }

         _dispatch(swap);
      }

      private static void _applyTitleBars()
      {
         Application? app = Application.Current;
         if (app is null)
         {
            return;
         }

         void apply()
         {
            foreach (Window window in app.Windows.Cast<Window>().ToArray())
            {
               DarkMode.SetImmersiveDarkMode(window, IsDarkAppearance);
            }
         }

         _dispatch(apply);
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
               if (window is IThemeAware windowAware)
               {
                  windowAware.OnThemeChanged();
               }

               if (window.DataContext is IThemeAware dataContextAware)
               {
                  dataContextAware.OnThemeChanged();
               }
            }
         }

         _dispatch(notify);
      }

      private static void _dispatch(Action action)
      {
         Application? app = Application.Current;
         if (app is null)
         {
            action();
            return;
         }

         if (app.Dispatcher.CheckAccess())
         {
            action();
         }
         else
         {
            app.Dispatcher.Invoke(action);
         }
      }

      private static bool _readWindowsAppsUseLightTheme()
      {
         try
         {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
               @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1;
         }
         catch (Exception ex)
            when (ex is SecurityException
            or IOException
            or UnauthorizedAccessException
            or ObjectDisposedException)
         {
            return false;
         }
      }
   }
}
