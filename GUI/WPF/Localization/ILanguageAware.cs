namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Surfaces with culture-dependent non-XAML strings (window titles, combo
   /// <c>ItemsSource</c>, computed labels) implement this so
   /// <see cref="LocalizationService.Apply"/> can refresh them without a restart.
   /// </summary>
   /// <remarks>
   /// Contract: every dialog <see cref="System.Windows.Window"/> subclass should
   /// implement this interface and, in <see cref="OnLanguageChanged"/>, call
   /// <see cref="LanguageAwareNotify.ForwardToDataContext"/> (or the typed
   /// ViewModel) before refreshing any code-behind snapshots. Prefer
   /// <c>{loc:Loc …}</c> for static XAML text — it refreshes via
   /// <see cref="TranslationSource"/> without this interface.
   /// </remarks>
   internal interface ILanguageAware
   {
      void OnLanguageChanged();
   }
}
