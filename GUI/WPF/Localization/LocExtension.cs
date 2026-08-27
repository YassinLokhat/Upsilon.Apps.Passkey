using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Upsilon.Apps.Passkey.GUI.WPF.Localization
{
   /// <summary>
   /// XAML markup: <c>{loc:Loc Menu_Save}</c> binds to <see cref="TranslationSource"/>
   /// so values update when the UI language changes (no restart).
   /// </summary>
   [MarkupExtensionReturnType(typeof(string))]
   internal sealed class LocExtension : MarkupExtension
   {
      public LocExtension(string key) => Key = key;

      public string Key { get; }

      public override object ProvideValue(IServiceProvider serviceProvider)
      {
         Binding binding = new($"[{Key}]")
         {
            Source = TranslationSource.Instance,
            Mode = BindingMode.OneWay,
         };

         // Design-time / template shared DP: still return a live Binding.
         if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target
            && target.TargetObject.GetType().FullName == "System.Windows.SharedDp")
         {
            return binding;
         }

         return binding.ProvideValue(serviceProvider);
      }
   }
}
