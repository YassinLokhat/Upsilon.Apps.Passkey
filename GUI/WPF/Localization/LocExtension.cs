using System.Windows.Data;
using System.Windows.Markup;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// XAML markup: <c>{loc:Loc Menu_Save}</c> binds to <see cref="TranslationSource"/>
   /// so values update when the UI language changes (no restart).
   /// </summary>
   [MarkupExtensionReturnType(typeof(string))]
   internal sealed class LocExtension(string key) : MarkupExtension
   {
      // Internal WPF type; not publicly accessible.
      private static readonly Type? _sharedDpType =
         typeof(Binding).Assembly.GetType("System.Windows.SharedDp");

      public string Key { get; } = key;

      public override object ProvideValue(IServiceProvider serviceProvider)
      {
         Binding binding = new($"[{Key}]")
         {
            Source = TranslationSource.Instance,
            Mode = BindingMode.OneWay,
         };

         // Design-time / template shared DP: still return a live Binding.
         return serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target
            && _sharedDpType is not null
            && _sharedDpType.IsInstanceOfType(target.TargetObject)
            ? binding
            : binding.ProvideValue(serviceProvider);
      }
   }
}
