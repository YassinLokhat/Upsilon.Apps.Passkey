using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;
using Upsilon.Apps.Passkey.GUI.WPF.Alerts;
using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.GUI.WPF.Themes
{
   /// <summary>
   /// Maps portable ViewModel states to the active theme brushes.
   /// </summary>
   internal static class VisualStateBrushes
   {
      public static Brush For(FieldVisualState state)
         => state switch
         {
            FieldVisualState.Dirty => FieldStateBrushes.ChangedBrush,
            FieldVisualState.Weak => SemanticBrushes.Warning,
            FieldVisualState.Leaked or FieldVisualState.WeakAndLeaked => SemanticBrushes.Danger,
            _ => FieldStateBrushes.UnchangedBrush2,
         };
   }

   /// <summary>
   /// Binds <see cref="FieldVisualState"/> to a background brush. Bindings are
   /// re-evaluated on theme change because ViewModels re-raise their state
   /// properties from <c>OnThemeChanged</c>.
   /// </summary>
   [ValueConversion(typeof(FieldVisualState), typeof(Brush))]
   internal sealed class FieldVisualStateToBrushConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => VisualStateBrushes.For(value is FieldVisualState state ? state : FieldVisualState.Default);

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => DependencyProperty.UnsetValue;
   }

   [ValueConversion(typeof(AlertSeverity), typeof(Brush))]
   internal sealed class AlertSeverityToBrushConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => AlertBroker.BrushFor(value is AlertSeverity severity ? severity : AlertSeverity.Info);

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => DependencyProperty.UnsetValue;
   }

   /// <summary>Visible when false, collapsed when true.</summary>
   [ValueConversion(typeof(bool), typeof(Visibility))]
   internal sealed class InverseBooleanToVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => value is true ? Visibility.Collapsed : Visibility.Visible;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => value is not Visibility.Visible;
   }
}
