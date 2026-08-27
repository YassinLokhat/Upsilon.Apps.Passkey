namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Windows or DataContexts that expose culture-dependent non-XAML strings
   /// (titles, combo ItemsSource, computed labels) implement this so
   /// <see cref="LocalizationService.Apply"/> can refresh them without a restart.
   /// </summary>
   internal interface ILanguageAware
   {
      void OnLanguageChanged();
   }
}
