using Upsilon.Apps.Passkey.GUI.Presentation.Ports;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Adapters
{
   internal sealed class MauiThemePlatform : IThemePlatform
   {
      public event EventHandler? SystemThemeChanged;

      public MauiThemePlatform()
      {
         if (Application.Current is { } app)
         {
            app.RequestedThemeChanged += (_, _) => SystemThemeChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      public void Apply(string themePreference)
      {
         if (Application.Current is null)
         {
            return;
         }

         Application.Current.UserAppTheme = themePreference?.Trim().ToUpperInvariant() switch
         {
            "LIGHT" => AppTheme.Light,
            "DARK" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
         };
      }
   }
}
