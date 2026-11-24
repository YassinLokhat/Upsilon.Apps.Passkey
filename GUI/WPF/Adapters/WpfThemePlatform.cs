using Upsilon.Apps.Passkey.GUI.Presentation.Ports;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.Adapters
{
   /// <summary>Shared theme port over <see cref="ThemeService"/> (which already follows OS changes).</summary>
   internal sealed class WpfThemePlatform : IThemePlatform
   {
      public event EventHandler? SystemThemeChanged
      {
         add { }
         remove { }
      }

      public void Apply(string themePreference)
         => _ = ThemeService.Apply(themePreference);
   }
}
