namespace Upsilon.Apps.Passkey.GUI.Presentation.Localization
{
   /// <summary>
   /// Shared ViewModel holding culture-dependent snapshots (titles, option lists,
   /// formatted rows). Hosts call <see cref="OnLanguageChanged"/> after a culture change.
   /// </summary>
   internal interface ILanguageRefreshable
   {
      void OnLanguageChanged();
   }
}
