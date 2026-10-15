using Upsilon.Apps.Passkey.GUI.Presentation.Localization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Localization
{
   /// <summary>
   /// XAML markup: <c>{loc:Loc Login_Next}</c> binds to <see cref="LocalizationCatalog"/>
   /// so values update when the UI language changes (no restart). Same contract
   /// as the WPF <c>LocExtension</c>.
   /// </summary>
   [ContentProperty(nameof(Key))]
   [AcceptEmptyServiceProvider]
   internal sealed class LocExtension : IMarkupExtension<BindingBase>
   {
      public string Key { get; set; } = string.Empty;

      public BindingBase ProvideValue(IServiceProvider serviceProvider)
         => new Binding($"[{Key}]", BindingMode.OneWay, source: LocalizationCatalog.Instance);

      object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
         => ProvideValue(serviceProvider);
   }
}
