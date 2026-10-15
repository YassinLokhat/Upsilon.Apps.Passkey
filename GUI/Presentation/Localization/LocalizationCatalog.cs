using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.Presentation.Localization
{
   /// <summary>
   /// Binding source for the hosts' localization markup extensions
   /// (<c>{loc:Loc Key}</c> in WPF and MAUI). Bind to the indexer
   /// <c>[Key]</c>; a culture change raises <see cref="PropertyChanged"/> with an
   /// empty name, which both WPF and MAUI treat as "every property changed", so
   /// all bindings refresh in place.
   /// </summary>
   internal sealed class LocalizationCatalog : INotifyPropertyChanged
   {
      public static LocalizationCatalog Instance { get; } = new();

      public string this[string key] => Strings.Get(key);

      public event PropertyChangedEventHandler? PropertyChanged;

      public void NotifyLanguageChanged()
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
   }
}
