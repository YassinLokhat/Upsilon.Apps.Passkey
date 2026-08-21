using System.Windows.Markup;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// XAML markup: <c>{loc:Loc Menu_Save}</c> resolves against <see cref="Strings"/>.
   /// Values are fixed at load time; change of language needs a restart for open windows.
   /// </summary>
   [MarkupExtensionReturnType(typeof(string))]
   internal sealed class LocExtension : MarkupExtension
   {
      public LocExtension(string key) => Key = key;

      public string Key { get; }

      public override object ProvideValue(IServiceProvider serviceProvider)
         => Strings.Get(Key);
   }
}
