namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// Windows or DataContexts that expose theme-dependent non-XAML values
   /// (code-behind brushes) implement this so <see cref="ThemeService.Apply"/>
   /// can refresh them without a restart.
   /// </summary>
   internal interface IThemeAware
   {
      void OnThemeChanged();
   }
}
