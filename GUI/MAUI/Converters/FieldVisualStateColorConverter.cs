using System.Globalization;
using Upsilon.Apps.Passkey.GUI.Presentation.Models;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Converters
{
   /// <summary>Maps a <see cref="FieldVisualState"/> to the entry text colour; <c>null</c> keeps the theme default.</summary>
   internal sealed class FieldVisualStateColorConverter : IValueConverter
   {
      public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
      {
         FieldVisualState.Dirty => Colors.DarkOrange,
         FieldVisualState.Weak => Colors.Goldenrod,
         FieldVisualState.Leaked or FieldVisualState.WeakAndLeaked => Colors.Red,
         _ => null,
      };

      public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
         => throw new NotSupportedException();
   }
}
