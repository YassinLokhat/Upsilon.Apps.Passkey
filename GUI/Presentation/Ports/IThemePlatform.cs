namespace Upsilon.Apps.Passkey.GUI.Presentation.Ports
{
   /// <summary>Host theme application (light / dark / follow OS).</summary>
   internal interface IThemePlatform
   {
      /// <summary>Effective preference: <c>System</c>, <c>Light</c>, or <c>Dark</c>.</summary>
      void Apply(string themePreference);

      /// <summary>Raised when the OS light/dark preference changes while effective theme is System.</summary>
      event EventHandler? SystemThemeChanged;
   }
}
