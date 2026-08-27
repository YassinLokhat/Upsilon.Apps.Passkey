using System.ComponentModel;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// Binding source for <see cref="LocExtension"/>. Raises <c>Item[]</c> when the
   /// UI culture changes so every <c>{loc:Loc …}</c> binding refreshes in place.
   /// </summary>
   internal sealed class TranslationSource : INotifyPropertyChanged
   {
      public static TranslationSource Instance { get; } = new();

      public string this[string key] => Strings.Get(key);

      public event PropertyChangedEventHandler? PropertyChanged;

      public void NotifyLanguageChanged()
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
   }
}
