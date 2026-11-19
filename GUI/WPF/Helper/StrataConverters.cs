using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Upsilon.Apps.Passkey.GUI.WPF.Themes;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// Upper-cases a (localized) caption at display time, e.g. the vault-layer
   /// notches and column captions ("VAULT", "7 SERVICES"). Resource strings keep
   /// their normal casing.
   /// </summary>
   internal sealed class UpperCaseConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => value is string text ? text.ToUpper(CultureInfo.CurrentUICulture) : value;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }

   /// <summary>
   /// True when a field's bound background is the "edited" brush
   /// (<see cref="FieldStateBrushes.ChangedBrush"/>). Lets the input template draw
   /// the edited border without a second bound property per field.
   /// </summary>
   internal sealed class IsChangedBrushConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => value is SolidColorBrush brush
            && FieldStateBrushes.ChangedBrush is SolidColorBrush changed
            && brush.Color == changed.Color;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }

   /// <summary>
   /// Scroll viewport height minus the 2 × 14 px margin of the account column, so
   /// its grid fills the viewport (notes take the rest) yet can still grow and
   /// scroll when the content is taller.
   /// </summary>
   internal sealed class ViewportMinusPaddingConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => value is double height ? Math.Max(0, height - 28) : 0d;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }

   /// <summary>Collapses an element when the bound count is zero (or not a number).</summary>
   internal sealed class CountToVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
         => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }
}
