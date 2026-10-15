using System.Globalization;
using Upsilon.Apps.Passkey.GUI.Presentation.Localization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   /// <summary>
   /// XAML markup: <c>{loc:Loc Login_Next}</c> binds to <see cref="LocalizationCatalog"/>
   /// so values update when the UI language changes (no restart). Same contract
   /// as the WPF <c>LocExtension</c>, minus the WPF access-key markers, which
   /// MAUI would display as is.
   /// </summary>
   [ContentProperty(nameof(Key))]
   [AcceptEmptyServiceProvider]
   internal sealed class LocExtension : IMarkupExtension<BindingBase>
   {
      public string Key { get; set; } = string.Empty;

      /// <summary>Same binding as the markup, for pages built in code.</summary>
      public static BindingBase For(string key)
         => new Binding($"[{key}]", BindingMode.OneWay, AccessKeyStripper.Instance, source: LocalizationCatalog.Instance);

      public BindingBase ProvideValue(IServiceProvider serviceProvider)
         => For(Key);

      object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
         => ProvideValue(serviceProvider);

      private sealed class AccessKeyStripper : IValueConverter
      {
         public static AccessKeyStripper Instance { get; } = new();

         public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string text ? AccessKeyText.Strip(text) : value;

         public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
      }
   }
}
