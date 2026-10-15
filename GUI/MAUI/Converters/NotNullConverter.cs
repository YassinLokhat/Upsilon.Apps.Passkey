using System.Globalization;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Converters
{
   internal sealed class NotNullConverter : IValueConverter
   {
      public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;

      public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }
}
