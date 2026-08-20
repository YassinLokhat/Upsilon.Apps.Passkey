using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Upsilon.Apps.Passkey.GUI.WPF.Helper
{
   /// <summary>
   /// INPC helper used by a few controls that do not inherit
   /// <see cref="ObservableObject"/> (legacy code-behind ViewModels).
   /// </summary>
   internal static class PropertyHelper
   {
      public static bool SetProperty<T>(ref T field,
         T newValue,
         INotifyPropertyChanged sender,
         PropertyChangedEventHandler? PropertyChanged,
         [CallerMemberName] string? propertyName = null)
      {
         if (!Equals(field, newValue))
         {
            field = newValue;
            PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));

            return true;
         }

         return false;
      }
   }
}
