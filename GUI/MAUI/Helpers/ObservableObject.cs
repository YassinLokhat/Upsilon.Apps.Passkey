using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Helpers
{
   internal abstract class ObservableObject : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler? PropertyChanged;

      protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, newValue))
         {
            return false;
         }

         field = newValue;
         OnPropertyChanged(propertyName);
         return true;
      }

      protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
}
